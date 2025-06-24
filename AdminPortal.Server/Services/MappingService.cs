using AdminPortal.Server.Services.Interfaces;
using System.Data.SqlClient;

namespace AdminPortal.Server.Services
{
    public sealed class MappingService : IMappingService
    {
        private readonly string? _connString;
        private readonly ILogger<MappingService> _logger;

        public MappingService(IConfiguration config, ILogger<MappingService> logger)
        {
            _connString = config.GetConnectionString("TCS");
            _logger = logger;
        }

        // HELPER: fetch correlated key + value name strings...
        private static (string keyCol, string valCol) Columns(string table) =>
            table switch
            {
                "COMPANY" => ("COMPANYKEY", "COMPANYNAME"),
                "MODULE" => ("MODULEURL", "MODULENAME"),
                _ => throw new ArgumentOutOfRangeException(nameof(table)),
            };

        // fetch company and module mapping dictionaries...
        public async Task<IDictionary<string, string>> GetCompaniesAsync() { return await ReadAsync("COMPANY"); }
        public async Task<IDictionary<string, string>> GetModulesAsync() { return await ReadAsync("MODULE"); }

        // read dictionary values from records...
        private async Task<IDictionary<string, string>> ReadAsync(string table)
        {
            await using var conn = new SqlConnection(_connString);
            await conn.OpenAsync();

            // grab all companies|modules currently active in records...
            await using var comm = new SqlCommand($"SELECT * FROM dbo.{table}", conn);

            // leverage helper to initialize table key + value names...
            string keyCol, valCol;
            (keyCol, valCol) = Columns(table);

            // initialize key + value dictionary to store mappings...
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // execute query and build dictionary until invalid read...
            await using var reader = await comm.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string? key = reader[keyCol]?.ToString();
                string? val = reader[valCol]?.ToString();
                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(val))
                {
                    dict[key!] = val!;
                }
            }

            return dict;
        }
    }
}