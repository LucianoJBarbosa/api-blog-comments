using Dapper;
using System.Data;
using System.Globalization;
using api_blog_comments_dev.Data.Migrations;
using api_blog_comments_dev.Services;

namespace api_blog_comments_dev.Data;

public sealed class DatabaseInitializer
{
    private readonly IDbConnectionFactory _connectionFactory;
    private const string SqliteProvider = "sqlite";
    private const string SqlServerProvider = "sqlserver";
    private static readonly IReadOnlyList<IDatabaseMigration> Migrations =
    [
        new InitialSchemaMigration(),
        new LegacyCompatibilityMigration()
    ];

    public DatabaseInitializer(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await EnsureMigrationHistoryAsync(connection, cancellationToken);
        var appliedMigrations = await GetAppliedMigrationsAsync(connection, cancellationToken);

        foreach (var migration in Migrations.Where(migration => !appliedMigrations.ContainsKey(migration.Id)))
        {
            await migration.ApplyAsync(connection, _connectionFactory.Provider, cancellationToken);
            await MarkMigrationAsAppliedAsync(connection, migration, cancellationToken);
        }
    }

    public async Task<DatabaseMigrationStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await MigrationHistoryExistsAsync(connection, cancellationToken))
        {
            return BuildStatus(new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase));
        }

        var appliedMigrations = await GetAppliedMigrationsAsync(connection, cancellationToken);
        return BuildStatus(appliedMigrations);
    }

    public IReadOnlyList<IDatabaseMigration> GetKnownMigrations() => Migrations;

    private async Task EnsureMigrationHistoryAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        var sql = _connectionFactory.Provider switch
        {
            SqliteProvider => """
                CREATE TABLE IF NOT EXISTS __SchemaMigrations (
                    Id TEXT NOT NULL PRIMARY KEY,
                    Description TEXT NOT NULL,
                    AppliedAt TEXT NOT NULL
                );
                """,
            SqlServerProvider => """
                IF OBJECT_ID(N'dbo.__SchemaMigrations', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.__SchemaMigrations (
                        Id NVARCHAR(200) NOT NULL PRIMARY KEY,
                        Description NVARCHAR(500) NOT NULL,
                        AppliedAt DATETIME2 NOT NULL
                    );
                END;
                """,
            _ => throw new InvalidOperationException($"Database provider '{_connectionFactory.Provider}' is not supported.")
        };

        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    private async Task<bool> MigrationHistoryExistsAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        var sql = _connectionFactory.Provider switch
        {
            SqliteProvider => "SELECT CASE WHEN EXISTS (SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = '__SchemaMigrations') THEN 1 ELSE 0 END;",
            SqlServerProvider => "SELECT CASE WHEN OBJECT_ID(N'dbo.__SchemaMigrations', N'U') IS NULL THEN 0 ELSE 1 END;",
            _ => throw new InvalidOperationException($"Database provider '{_connectionFactory.Provider}' is not supported.")
        };

        var exists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return exists == 1;
    }

    private async Task<Dictionary<string, DateTime>> GetAppliedMigrationsAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        var tableName = _connectionFactory.Provider == SqlServerProvider ? "dbo.__SchemaMigrations" : "__SchemaMigrations";
        var sql = _connectionFactory.Provider switch
        {
            SqliteProvider => $"SELECT Id, AppliedAt FROM {tableName};",
            SqlServerProvider => $"SELECT Id, CONVERT(NVARCHAR(50), AppliedAt, 127) AS AppliedAt FROM {tableName};",
            _ => throw new InvalidOperationException($"Database provider '{_connectionFactory.Provider}' is not supported.")
        };
        var appliedMigrations = await connection.QueryAsync<AppliedMigrationRecord>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return appliedMigrations.ToDictionary(
            migration => migration.Id,
            migration => DateTime.Parse(migration.AppliedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            StringComparer.OrdinalIgnoreCase);
    }

    private async Task MarkMigrationAsAppliedAsync(IDbConnection connection, IDatabaseMigration migration, CancellationToken cancellationToken)
    {
        var sql = _connectionFactory.Provider switch
        {
            SqliteProvider => "INSERT INTO __SchemaMigrations (Id, Description, AppliedAt) VALUES (@Id, @Description, @AppliedAt);",
            SqlServerProvider => "INSERT INTO dbo.__SchemaMigrations (Id, Description, AppliedAt) VALUES (@Id, @Description, @AppliedAt);",
            _ => throw new InvalidOperationException($"Database provider '{_connectionFactory.Provider}' is not supported.")
        };

        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            migration.Id,
            migration.Description,
            AppliedAt = DateTime.UtcNow
        }, cancellationToken: cancellationToken));
    }

    private DatabaseMigrationStatus BuildStatus(IReadOnlyDictionary<string, DateTime> appliedMigrations)
    {
        var migrationStates = Migrations
            .Select(migration => new DatabaseMigrationState(
                migration.Id,
                migration.Description,
                appliedMigrations.TryGetValue(migration.Id, out var appliedAt),
                appliedMigrations.TryGetValue(migration.Id, out appliedAt) ? appliedAt : null))
            .ToArray();

        return new DatabaseMigrationStatus(_connectionFactory.Provider, migrationStates);
    }

    private sealed record AppliedMigrationRecord(string Id, string AppliedAt);
}