using Dapper;
using Npgsql;
using System.Globalization;
using System.IO;
using WpfTcpServer;

namespace WpfTcpServer
{
    public class DatabaseManager
    {
        private readonly string _connectionString;

        private const int IdleThresholdSeconds = 60;

        private const int RulesCacheSeconds = 30;

        private const int LongIdleViolationSeconds = 300;

        private DateTime _rulesLoadedAt = DateTime.MinValue;

        private List<ControlRule> _applicationRules = new();
        private List<ControlRule> _webResourceRules = new();

        private class ControlRule
        {
            public string Value { get; set; } = "";
            public string RuleType { get; set; } = "";
        }

        private class ActivityPeriodRow
        {
            public int Id { get; set; }
            public int PeriodTypeId { get; set; }
            public string PeriodTypeName { get; set; } = "";
            public DateTime StartTime { get; set; }
            public DateTime? EndTime { get; set; }
        }

        public DatabaseManager()
        {
            _connectionString = "Host=localhost;Port=5432;Database=1984;Username=postgres;Password=12345678;Pooling=true;";
        }

        public async Task<int> AddOrUpdateWorkstationAsync(ClientInfo client)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                INSERT INTO workstations 
                    (ip_address, username, domain_name, host_name)
                VALUES 
                    (@IP, @UserName, @DomainName, @HostName)
                ON CONFLICT (ip_address)
                DO UPDATE SET
                    username = EXCLUDED.username,
                    domain_name = EXCLUDED.domain_name,
                    host_name = EXCLUDED.host_name
                RETURNING id;
            ";

            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                client.IP,
                client.UserName,
                client.DomainName,
                client.HostName
            });
        }

        public async Task AddActivityEventAsync(int workstationId, string lastActiveTime)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();

            DateTime eventTime = DateTime.ParseExact(
                lastActiveTime,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture
            );

            string activitySql = @"
                INSERT INTO activity_events 
                    (workstation_id, last_active_time)
                VALUES 
                    (@WorkstationId, @LastActiveTime)
                ON CONFLICT (workstation_id)
                DO UPDATE SET 
                    last_active_time = EXCLUDED.last_active_time;
            ";

            await connection.ExecuteAsync(activitySql, new
            {
                WorkstationId = workstationId,
                LastActiveTime = eventTime
            }, transaction);

            await UpdateActivityPeriodsAsync(connection, transaction, workstationId, eventTime);

            await transaction.CommitAsync();
        }

        private async Task UpdateActivityPeriodsAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            int workstationId,
            DateTime eventTime)
        {
            string selectSql = @"
                SELECT 
                    ap.id AS Id,
                    ap.period_type_id AS PeriodTypeId,
                    apt.name AS PeriodTypeName,
                    ap.start_time AS StartTime,
                    ap.end_time AS EndTime
                FROM activity_periods ap
                JOIN activity_period_types apt ON ap.period_type_id = apt.id
                WHERE ap.workstation_id = @WorkstationId
                ORDER BY ap.start_time DESC, ap.id DESC
                LIMIT 1;
            ";

            ActivityPeriodRow? lastPeriod = await connection.QueryFirstOrDefaultAsync<ActivityPeriodRow>(
                selectSql,
                new { WorkstationId = workstationId },
                transaction
            );

            if (lastPeriod == null)
            {
                await CreatePeriodAsync(connection, transaction, workstationId, "active", eventTime, eventTime);
                return;
            }

            DateTime previousEndTime = lastPeriod.EndTime ?? lastPeriod.StartTime;

            if (eventTime <= previousEndTime)
                return;

            int gapSeconds = (int)(eventTime - previousEndTime).TotalSeconds;

            if (lastPeriod.PeriodTypeName == "active")
            {
                if (gapSeconds <= IdleThresholdSeconds)
                {
                    await UpdatePeriodEndAsync(connection, transaction, lastPeriod.Id, eventTime, lastPeriod.StartTime);
                }
                else
                {
                    int idlePeriodId = await CreatePeriodAsync(
                        connection,
                        transaction,
                        workstationId,
                        "idle",
                        previousEndTime,
                        eventTime
                    );

                    if (gapSeconds >= LongIdleViolationSeconds)
                    {
                        await AddIdleViolationAsync(
                            connection,
                            transaction,
                            workstationId,
                            idlePeriodId,
                            gapSeconds,
                            previousEndTime,
                            eventTime
                        );
                    }

                    await CreatePeriodAsync(connection, transaction, workstationId, "active", eventTime, eventTime);
                }
            }
            else if (lastPeriod.PeriodTypeName == "idle")
            {
                await UpdatePeriodEndAsync(connection, transaction, lastPeriod.Id, eventTime, lastPeriod.StartTime);
                await CreatePeriodAsync(connection, transaction, workstationId, "active", eventTime, eventTime);
            }
        }

        private async Task<int> CreatePeriodAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            int workstationId,
            string periodType,
            DateTime startTime,
            DateTime endTime)
        {
            int durationSeconds = (int)(endTime - startTime).TotalSeconds;

            string sql = @"
                INSERT INTO activity_periods
                    (workstation_id, period_type_id, start_time, end_time, duration_seconds)
                VALUES
                    (
                        @WorkstationId,
                        (SELECT id FROM activity_period_types WHERE name = @PeriodType),
                        @StartTime,
                        @EndTime,
                        @DurationSeconds
                    )
                RETURNING id;
            ";

            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                WorkstationId = workstationId,
                PeriodType = periodType,
                StartTime = startTime,
                EndTime = endTime,
                DurationSeconds = durationSeconds
            }, transaction);
        }

        private async Task UpdatePeriodEndAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            int periodId,
            DateTime endTime,
            DateTime startTime)
        {
            int durationSeconds = (int)(endTime - startTime).TotalSeconds;

            string sql = @"
                UPDATE activity_periods
                SET 
                    end_time = @EndTime,
                    duration_seconds = @DurationSeconds
                WHERE id = @PeriodId;
            ";

            await connection.ExecuteAsync(sql, new
            {
                PeriodId = periodId,
                EndTime = endTime,
                DurationSeconds = durationSeconds
            }, transaction);
        }

        public async Task AddScreenshotAsync(int workstationId, string filePath)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                INSERT INTO screenshots
                    (workstation_id, file_path, created_at)
                VALUES
                    (@WorkstationId, @FilePath, CURRENT_TIMESTAMP);
            ";

            await connection.ExecuteAsync(sql, new
            {
                WorkstationId = workstationId,
                FilePath = filePath
            });
        }

        public async Task<int> CreateWindowActivityAsync(
            int workstationId,
            string windowTitle,
            string processName,
            int processId,
            DateTime startTime,
            DateTime endTime)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            int durationSeconds = (int)(endTime - startTime).TotalSeconds;

            string sql = @"
                INSERT INTO window_activity
                    (workstation_id, window_title, process_name, process_id, start_time, end_time, duration_seconds)
                VALUES
                    (@WorkstationId, @WindowTitle, @ProcessName, @ProcessId, @StartTime, @EndTime, @DurationSeconds)
                RETURNING id;
            ";

            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                WorkstationId = workstationId,
                WindowTitle = windowTitle,
                ProcessName = processName,
                ProcessId = processId,
                StartTime = startTime,
                EndTime = endTime,
                DurationSeconds = durationSeconds
            });
        }

        public async Task UpdateWindowActivityAsync(
            int windowActivityId,
            DateTime startTime,
            DateTime endTime)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            int durationSeconds = (int)(endTime - startTime).TotalSeconds;

            string sql = @"
                UPDATE window_activity
                SET 
                    end_time = @EndTime,
                    duration_seconds = @DurationSeconds
                WHERE id = @Id;
            ";

            await connection.ExecuteAsync(sql, new
            {
                Id = windowActivityId,
                EndTime = endTime,
                DurationSeconds = durationSeconds
            });
        }

        public async Task<int> AddWebActivityAsync(
            int workstationId,
            string processName,
            string windowTitle,
            string domainName,
            string detectionMethod,
            DateTime accessTime)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            detectionMethod = "window_title";

            string sql = @"
                INSERT INTO web_activity
                    (workstation_id, process_name, window_title, domain_name, detection_method, access_time)
                VALUES
                    (@WorkstationId, @ProcessName, @WindowTitle, @DomainName, @DetectionMethod, @AccessTime)
                RETURNING id;
            ";

            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                WorkstationId = workstationId,
                ProcessName = processName,
                WindowTitle = windowTitle,
                DomainName = domainName,
                DetectionMethod = detectionMethod,
                AccessTime = accessTime
            });
        }


        public async Task AddDnsCacheRecordAsync(
            int workstationId,
            string domainName,
            string resolvedIp,
            DateTime recordTime)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                INSERT INTO dns_cache_records
                    (workstation_id, domain_name, resolved_ip, record_time)
                SELECT
                    @WorkstationId, @DomainName, @ResolvedIp, @RecordTime
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM dns_cache_records
                    WHERE workstation_id = @WorkstationId
                      AND domain_name = @DomainName
                      AND COALESCE(resolved_ip, '') = COALESCE(@ResolvedIp, '')
                      AND record_time >= @RecordTime - INTERVAL '2 minutes'
                );
            ";

            await connection.ExecuteAsync(sql, new
            {
                WorkstationId = workstationId,
                DomainName = domainName,
                ResolvedIp = resolvedIp,
                RecordTime = recordTime
            });
        }

        public async Task RefineRecentWebActivityFromDnsAsync(
            int workstationId,
            DateTime eventTime)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                WITH candidate_dns AS (
                    SELECT domain_name
                    FROM dns_cache_records
                    WHERE workstation_id = @WorkstationId
                        AND record_time BETWEEN @EventTime - INTERVAL '30 seconds'
                                            AND @EventTime + INTERVAL '30 seconds'
                        AND domain_name NOT ILIKE '%windows%'
                        AND domain_name NOT ILIKE '%microsoft%'
                        AND domain_name NOT ILIKE '%msft%'
                        AND domain_name NOT ILIKE '%doubleclick%'
                        AND domain_name NOT ILIKE '%google-analytics%'
                        AND domain_name NOT ILIKE '%gstatic%'
                        AND domain_name NOT ILIKE '%cloudflare%'
                        AND domain_name NOT ILIKE '%akamai%'
                        AND domain_name NOT ILIKE '%cdn%'
                    ORDER BY record_time DESC
                    LIMIT 1
                )
                UPDATE web_activity
                SET
                    domain_name = (SELECT domain_name FROM candidate_dns),
                    detection_method = 'combined'
                WHERE workstation_id = @WorkstationId
                    AND domain_name = 'unknown'
                    AND access_time BETWEEN @EventTime - INTERVAL '30 seconds'
                                        AND @EventTime + INTERVAL '30 seconds'
                    AND EXISTS (SELECT 1 FROM candidate_dns);
            ";

            await connection.ExecuteAsync(sql, new
            {
                WorkstationId = workstationId,
                EventTime = eventTime
            });
        }

        private async Task EnsureRulesLoadedAsync()
        {
            if ((DateTime.Now - _rulesLoadedAt).TotalSeconds < RulesCacheSeconds)
                return;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string appSql = @"
        SELECT 
            LOWER(application_name) AS Value,
            LOWER(rt.name) AS RuleType
        FROM application_rules ar
        JOIN rule_types rt ON ar.rule_type_id = rt.id
        WHERE ar.is_active = true;
    ";

            string webSql = @"
        SELECT 
            LOWER(domain_name) AS Value,
            LOWER(rt.name) AS RuleType
        FROM web_resource_rules wr
        JOIN rule_types rt ON wr.rule_type_id = rt.id
        WHERE wr.is_active = true;
    ";

            _applicationRules = (await connection.QueryAsync<ControlRule>(appSql)).ToList();
            _webResourceRules = (await connection.QueryAsync<ControlRule>(webSql)).ToList();

            _rulesLoadedAt = DateTime.Now;
        }

        public async Task<string> CheckApplicationRuleAsync(string processName)
        {
            await EnsureRulesLoadedAsync();

            if (string.IsNullOrWhiteSpace(processName))
                return "none";

            string normalizedProcessName = Path.GetFileName(processName)
                .Trim()
                .ToLowerInvariant();

            ControlRule? blacklistRule = _applicationRules.FirstOrDefault(rule =>
                rule.RuleType == "blacklist" &&
                normalizedProcessName == rule.Value
            );

            if (blacklistRule != null)
                return "blacklist";

            ControlRule? whitelistRule = _applicationRules.FirstOrDefault(rule =>
                rule.RuleType == "whitelist" &&
                normalizedProcessName == rule.Value
            );

            if (whitelistRule != null)
                return "whitelist";

            return "none";
        }

        public async Task<string> CheckWebResourceRuleAsync(string domainName)
        {
            await EnsureRulesLoadedAsync();

            if (string.IsNullOrWhiteSpace(domainName))
                return "none";

            if (domainName == "unknown")
                return "none";

            string normalizedDomain = domainName
                .Trim()
                .Trim('.')
                .ToLowerInvariant();

            ControlRule? blacklistRule = _webResourceRules.FirstOrDefault(rule =>
                rule.RuleType == "blacklist" &&
                IsDomainMatch(normalizedDomain, rule.Value)
            );

            if (blacklistRule != null)
                return "blacklist";

            ControlRule? whitelistRule = _webResourceRules.FirstOrDefault(rule =>
                rule.RuleType == "whitelist" &&
                IsDomainMatch(normalizedDomain, rule.Value)
            );

            if (whitelistRule != null)
                return "whitelist";

            return "none";
        }

        private static bool IsDomainMatch(string domain, string ruleDomain)
        {
            if (string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(ruleDomain))
                return false;

            ruleDomain = ruleDomain.Trim().Trim('.').ToLowerInvariant();

            return domain == ruleDomain || domain.EndsWith("." + ruleDomain);
        }

        public async Task<List<RuleViewModel>> LoadApplicationRulesAsync()
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                SELECT
                    ar.id AS Id,
                    ar.application_name AS Value,
                    rt.name AS RuleType,
                    ar.is_active AS IsActive,
                    ar.created_at AS CreatedAt
                FROM application_rules ar
                JOIN rule_types rt ON ar.rule_type_id = rt.id
                ORDER BY ar.id;
            ";

            return (await connection.QueryAsync<RuleViewModel>(sql)).ToList();
        }

        public async Task<List<RuleViewModel>> LoadWebResourceRulesAsync()
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                SELECT
                    wr.id AS Id,
                    wr.domain_name AS Value,
                    rt.name AS RuleType,
                    wr.is_active AS IsActive,
                    wr.created_at AS CreatedAt
                FROM web_resource_rules wr
                JOIN rule_types rt ON wr.rule_type_id = rt.id
                ORDER BY wr.id;
            ";

            return (await connection.QueryAsync<RuleViewModel>(sql)).ToList();
        }

        public async Task AddApplicationRuleAsync(string applicationName, string ruleType)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                INSERT INTO application_rules
                    (application_name, rule_type_id, is_active, created_at)
                VALUES
                    (
                        @ApplicationName,
                        (SELECT id FROM rule_types WHERE name = @RuleType),
                        true,
                        CURRENT_TIMESTAMP
                    );
            ";

            await connection.ExecuteAsync(sql, new
            {
                ApplicationName = applicationName,
                RuleType = ruleType
            });
        }

        public async Task AddWebResourceRuleAsync(string domainName, string ruleType)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                INSERT INTO web_resource_rules
                    (domain_name, rule_type_id, is_active, created_at)
                VALUES
                    (
                        @DomainName,
                        (SELECT id FROM rule_types WHERE name = @RuleType),
                        true,
                        CURRENT_TIMESTAMP
                    );
            ";

            await connection.ExecuteAsync(sql, new
            {
                DomainName = domainName,
                RuleType = ruleType
            });
        }

        public async Task DeleteApplicationRuleAsync(int ruleId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                DELETE FROM application_rules
                WHERE id = @RuleId;
            ";

            await connection.ExecuteAsync(sql, new { RuleId = ruleId });
        }

        public async Task DeleteWebResourceRuleAsync(int ruleId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                DELETE FROM web_resource_rules
                WHERE id = @RuleId;
            ";

            await connection.ExecuteAsync(sql, new { RuleId = ruleId });
        }

        public async Task UpdateApplicationRuleActiveAsync(int ruleId, bool isActive)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                UPDATE application_rules
                SET is_active = @IsActive
                WHERE id = @RuleId;
            ";

            await connection.ExecuteAsync(sql, new
            {
                RuleId = ruleId,
                IsActive = isActive
            });
        }

        public async Task UpdateWebResourceRuleActiveAsync(int ruleId, bool isActive)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                UPDATE web_resource_rules
                SET is_active = @IsActive
                WHERE id = @RuleId;
            ";

            await connection.ExecuteAsync(sql, new
            {
                RuleId = ruleId,
                IsActive = isActive
            });
        }

        public async Task AddViolationAsync(
            int workstationId,
            string sourceTable,
            int? sourceEventId,
            string violationType,
            string severity,
            string description,
            string relatedEntity,
            string ruleSource,
            DateTime detectedAt)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                INSERT INTO violations
                    (
                        workstation_id,
                        source_table,
                        source_event_id,
                        violation_type,
                        severity,
                        description,
                        related_entity,
                        rule_source,
                        detected_at,
                        is_resolved
                    )
                SELECT
                    @WorkstationId,
                    @SourceTable,
                    @SourceEventId,
                    @ViolationType,
                    @Severity,
                    @Description,
                    @RelatedEntity,
                    @RuleSource,
                    @DetectedAt,
                    false
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM violations
                    WHERE workstation_id = @WorkstationId
                      AND source_table = @SourceTable
                      AND COALESCE(source_event_id, 0) = COALESCE(@SourceEventId, 0)
                      AND violation_type = @ViolationType
                      AND related_entity = @RelatedEntity
                      AND detected_at >= @DetectedAt - INTERVAL '1 minute'
                );
            ";

            await connection.ExecuteAsync(sql, new
            {
                WorkstationId = workstationId,
                SourceTable = sourceTable,
                SourceEventId = sourceEventId,
                ViolationType = violationType,
                Severity = severity,
                Description = description,
                RelatedEntity = relatedEntity,
                RuleSource = ruleSource,
                DetectedAt = detectedAt
            });
        }

        private async Task AddIdleViolationAsync(
    NpgsqlConnection connection,
    NpgsqlTransaction transaction,
    int workstationId,
    int activityPeriodId,
    int durationSeconds,
    DateTime startTime,
    DateTime detectedAt)
        {
            string sql = @"
                INSERT INTO violations
                    (
                        workstation_id,
                        source_table,
                        source_event_id,
                        violation_type,
                        severity,
                        description,
                        related_entity,
                        rule_source,
                        detected_at,
                        is_resolved
                    )
                SELECT
                    @WorkstationId,
                    'activity_periods',
                    @ActivityPeriodId,
                    'long_idle',
                    'low',
                    @Description,
                    @RelatedEntity,
                    'activity_periods',
                    @DetectedAt,
                    false
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM violations
                    WHERE workstation_id = @WorkstationId
                      AND source_table = 'activity_periods'
                      AND source_event_id = @ActivityPeriodId
                      AND violation_type = 'long_idle'
                );
            ";

            await connection.ExecuteAsync(sql, new
            {
                WorkstationId = workstationId,
                ActivityPeriodId = activityPeriodId,
                Description = $"Зафиксирован длительный простой: {durationSeconds} секунд",
                RelatedEntity = $"idle_from_{startTime:yyyy-MM-dd HH:mm:ss}",
                DetectedAt = detectedAt
            }, transaction);
        }
    }
}