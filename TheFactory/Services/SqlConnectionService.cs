using Microsoft.Data.SqlClient;

public class SqlConnectionService
{
    private readonly IConfiguration _configuration;

    public SqlConnectionService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<SqlConnection> GetSqlConnectionAsync()
    {
        var connectionString = _configuration.GetConnectionString("AzureSqlDb");
        var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        return conn;
    }
}