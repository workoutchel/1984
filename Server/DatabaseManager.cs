using Dapper;
using Npgsql;
using System.Globalization;

namespace Server
{
    public class DatabaseManager
    {
        private readonly string _connectionString;

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

            DateTime parsedTime = DateTime.ParseExact(
                lastActiveTime,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture
            );

            string sql = @"
            INSERT INTO activity_events 
                (workstation_id, last_active_time)
            VALUES 
                (@WorkstationId, @LastActiveTime)
            ON CONFLICT (workstation_id)
            DO UPDATE SET 
                last_active_time = EXCLUDED.last_active_time;
            ";

            await connection.ExecuteAsync(sql, new
            {
                WorkstationId = workstationId,
                LastActiveTime = parsedTime
            });
        }
    }
}