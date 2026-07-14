using Azure.Core;
using Microsoft.Data.SqlClient;

public sealed class SqlConnectionFactory
{
    private static readonly string[] AzureSqlScopes = ["https://database.windows.net//.default"];

    private readonly string _connectionString;
    private readonly string _sqlClientConnectionString;
    private readonly TokenCredential _tokenCredential;
    private readonly bool _usesActiveDirectoryDefault;

    public SqlConnectionFactory(IConfiguration configuration, TokenCredential tokenCredential)
    {
        _connectionString = configuration.GetConnectionString("AzureSqlDb")
            ?? throw new InvalidOperationException("Connection string 'AzureSqlDb' was not found.");
        _usesActiveDirectoryDefault = _connectionString.Contains(
            "Authentication=Active Directory Default",
            StringComparison.OrdinalIgnoreCase);
        _sqlClientConnectionString = BuildSqlClientConnectionString(_connectionString, _usesActiveDirectoryDefault);
        _tokenCredential = tokenCredential;
    }

    public SqlConnection CreateConnection()
    {
        var connection = new SqlConnection(_sqlClientConnectionString);

        if (_usesActiveDirectoryDefault)
        {
            connection.AccessToken = _tokenCredential.GetToken(
                new TokenRequestContext(AzureSqlScopes),
                CancellationToken.None).Token;
        }

        return connection;
    }

    public async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_sqlClientConnectionString);

        if (_usesActiveDirectoryDefault)
        {
            connection.AccessToken = (await _tokenCredential.GetTokenAsync(
                new TokenRequestContext(AzureSqlScopes),
                cancellationToken)).Token;
        }

        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static string BuildSqlClientConnectionString(string connectionString, bool usesActiveDirectoryDefault)
    {
        if (!usesActiveDirectoryDefault)
        {
            return connectionString;
        }

        var builder = new SqlConnectionStringBuilder(connectionString);
        builder.Remove("Authentication");
        return builder.ConnectionString;
    }
}