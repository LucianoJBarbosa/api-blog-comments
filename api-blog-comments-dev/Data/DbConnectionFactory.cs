using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace api_blog_comments_dev.Data;

public interface IDbConnectionFactory
{
    string Provider { get; }
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}

public sealed class DbConnectionFactory : IDbConnectionFactory
{
    private const string SqliteProvider = "sqlite";
    private const string SqlServerProvider = "sqlserver";

    private readonly string _connectionString;
    public string Provider { get; }

    public DbConnectionFactory(IConfiguration configuration)
    {
        Provider = configuration["Database:Provider"]?.Trim().ToLowerInvariant() ?? SqliteProvider;
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=api-blog-comments.db";
    }

    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        IDbConnection connection = Provider switch
        {
            SqliteProvider => new SqliteConnection(_connectionString),
            SqlServerProvider => new SqlConnection(_connectionString),
            _ => throw new InvalidOperationException($"Database provider '{Provider}' is not supported. Use 'Sqlite' or 'SqlServer'.")
        };

        switch (connection)
        {
            case SqliteConnection sqliteConnection:
                await sqliteConnection.OpenAsync(cancellationToken);

                await using (var pragmaCommand = sqliteConnection.CreateCommand())
                {
                    pragmaCommand.CommandText = "PRAGMA foreign_keys = ON;";
                    await pragmaCommand.ExecuteNonQueryAsync(cancellationToken);
                }
                break;
            case SqlConnection sqlConnection:
                await sqlConnection.OpenAsync(cancellationToken);
                break;
        }

        return connection;
    }
}