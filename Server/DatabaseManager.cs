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
            public string PeriodType { get; set; } = "";
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
            id AS Id,
            period_type AS PeriodType,
            start_time AS StartTime,
            end_time AS EndTime
        FROM activity_periods
        WHERE workstation_id = @WorkstationId
        ORDER BY start_time DESC, id DESC
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

            if (lastPeriod.PeriodType == "active")
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
            else if (lastPeriod.PeriodType == "idle")
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
                (workstation_id, period_type, start_time, end_time, duration_seconds)
            VALUES
                (@WorkstationId, @PeriodType, @StartTime, @EndTime, @DurationSeconds);
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


    }
}