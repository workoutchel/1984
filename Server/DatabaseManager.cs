using Dapper;
using Npgsql;
using System.Globalization;

namespace Server
{

    public class DatabaseManager
    {
        private readonly string _connectionString;

        // Период
        private const int IdleThresholdSeconds = 60;

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

            int workstationId = await connection.ExecuteScalarAsync<int>(sql, new
            {
                client.IP,
                client.UserName,
                client.DomainName,
                client.HostName
            });

            return workstationId;
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
                    await CreatePeriodAsync(connection, transaction, workstationId, "idle", previousEndTime, eventTime);
                    await CreatePeriodAsync(connection, transaction, workstationId, "active", eventTime, eventTime);
                }
            }
            else if (lastPeriod.PeriodTypeName == "idle")
            {
                await UpdatePeriodEndAsync(connection, transaction, lastPeriod.Id, eventTime, lastPeriod.StartTime);
                await CreatePeriodAsync(connection, transaction, workstationId, "active", eventTime, eventTime);
            }
        }

        private async Task CreatePeriodAsync(
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
                    );
            ";

            await connection.ExecuteAsync(sql, new
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


    }
}