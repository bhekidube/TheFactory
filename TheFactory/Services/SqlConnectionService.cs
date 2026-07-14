using Microsoft.Data.SqlClient;

public class SqlConnectionService
{
    private readonly SqlConnectionFactory _connectionFactory;

    public SqlConnectionService(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<SqlConnection> GetSqlConnectionAsync()
    {
        return await _connectionFactory.OpenConnectionAsync();
    }
}