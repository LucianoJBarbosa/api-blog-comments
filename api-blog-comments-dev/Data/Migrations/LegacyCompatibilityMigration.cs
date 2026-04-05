using System.Data;
using Dapper;

namespace api_blog_comments_dev.Data.Migrations;

public sealed class LegacyCompatibilityMigration : IDatabaseMigration
{
    private const string SqliteProvider = "sqlite";
    private const string SqlServerProvider = "sqlserver";

    public string Id => "0002_legacy_compatibility";
    public string Description => "Backfills legacy databases with audit columns and indexes.";

    public async Task ApplyAsync(IDbConnection connection, string provider, CancellationToken cancellationToken = default)
    {
        switch (provider)
        {
            case SqliteProvider:
                await ApplySqliteAsync(connection, cancellationToken);
                return;
            case SqlServerProvider:
                await ApplySqlServerAsync(connection, cancellationToken);
                return;
            default:
                throw new InvalidOperationException($"Database provider '{provider}' is not supported.");
        }
    }

    private static async Task ApplySqliteAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        var compatibilityCommands = new[]
        {
            "ALTER TABLE Users ADD COLUMN Role TEXT NOT NULL DEFAULT 'Author';",
            "ALTER TABLE Users ADD COLUMN CreatedAt TEXT NULL;",
            "ALTER TABLE Users ADD COLUMN UpdatedAt TEXT NULL;",
            "ALTER TABLE BlogPosts ADD COLUMN CreatedByUserId INTEGER NULL;",
            "ALTER TABLE BlogPosts ADD COLUMN CreatedAt TEXT NULL;",
            "ALTER TABLE BlogPosts ADD COLUMN UpdatedAt TEXT NULL;",
            "ALTER TABLE Comments ADD COLUMN CreatedByUserId INTEGER NULL;",
            "ALTER TABLE Comments ADD COLUMN CreatedAt TEXT NULL;",
            "ALTER TABLE Comments ADD COLUMN UpdatedAt TEXT NULL;"
        };

        foreach (var compatibilityCommand in compatibilityCommands)
        {
            try
            {
                await connection.ExecuteAsync(new CommandDefinition(compatibilityCommand, cancellationToken: cancellationToken));
            }
            catch
            {
                // Banco legado pode já ter recebido parte das colunas em execuções anteriores.
            }
        }

        const string backfillSql = """
            UPDATE Users
            SET Role = 'Author'
            WHERE Role IS NULL OR Role = '';

            UPDATE Users
            SET CreatedAt = COALESCE(CreatedAt, CURRENT_TIMESTAMP),
                UpdatedAt = COALESCE(UpdatedAt, CURRENT_TIMESTAMP);

            UPDATE BlogPosts
            SET CreatedAt = COALESCE(CreatedAt, CURRENT_TIMESTAMP),
                UpdatedAt = COALESCE(UpdatedAt, CURRENT_TIMESTAMP);

            UPDATE Comments
            SET CreatedAt = COALESCE(CreatedAt, CURRENT_TIMESTAMP),
                UpdatedAt = COALESCE(UpdatedAt, CURRENT_TIMESTAMP);

            CREATE INDEX IF NOT EXISTS IX_BlogPosts_CreatedByUserId ON BlogPosts (CreatedByUserId);
            CREATE INDEX IF NOT EXISTS IX_BlogPosts_CreatedAt ON BlogPosts (CreatedAt DESC);
            CREATE INDEX IF NOT EXISTS IX_Comments_BlogPostId ON Comments (BlogPostId);
            CREATE INDEX IF NOT EXISTS IX_Comments_CreatedByUserId ON Comments (CreatedByUserId);
            CREATE INDEX IF NOT EXISTS IX_Comments_CreatedAt ON Comments (CreatedAt DESC);
            """;

        await connection.ExecuteAsync(new CommandDefinition(backfillSql, cancellationToken: cancellationToken));
    }

    private static async Task ApplySqlServerAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            IF COL_LENGTH('dbo.Users', 'Role') IS NULL
                ALTER TABLE dbo.Users ADD Role NVARCHAR(50) NOT NULL CONSTRAINT DF_Users_Role DEFAULT 'Author';

            IF COL_LENGTH('dbo.Users', 'CreatedAt') IS NULL
                ALTER TABLE dbo.Users ADD CreatedAt DATETIME2 NULL;

            IF COL_LENGTH('dbo.Users', 'UpdatedAt') IS NULL
                ALTER TABLE dbo.Users ADD UpdatedAt DATETIME2 NULL;

            IF COL_LENGTH('dbo.BlogPosts', 'CreatedByUserId') IS NULL
                ALTER TABLE dbo.BlogPosts ADD CreatedByUserId INT NULL;

            IF COL_LENGTH('dbo.BlogPosts', 'CreatedAt') IS NULL
                ALTER TABLE dbo.BlogPosts ADD CreatedAt DATETIME2 NULL;

            IF COL_LENGTH('dbo.BlogPosts', 'UpdatedAt') IS NULL
                ALTER TABLE dbo.BlogPosts ADD UpdatedAt DATETIME2 NULL;

            IF COL_LENGTH('dbo.Comments', 'CreatedByUserId') IS NULL
                ALTER TABLE dbo.Comments ADD CreatedByUserId INT NULL;

            IF COL_LENGTH('dbo.Comments', 'CreatedAt') IS NULL
                ALTER TABLE dbo.Comments ADD CreatedAt DATETIME2 NULL;

            IF COL_LENGTH('dbo.Comments', 'UpdatedAt') IS NULL
                ALTER TABLE dbo.Comments ADD UpdatedAt DATETIME2 NULL;

            UPDATE dbo.Users SET Role = 'Author' WHERE Role IS NULL OR Role = '';
            UPDATE dbo.Users SET CreatedAt = ISNULL(CreatedAt, SYSUTCDATETIME()), UpdatedAt = ISNULL(UpdatedAt, SYSUTCDATETIME());
            UPDATE dbo.BlogPosts SET CreatedAt = ISNULL(CreatedAt, SYSUTCDATETIME()), UpdatedAt = ISNULL(UpdatedAt, SYSUTCDATETIME());
            UPDATE dbo.Comments SET CreatedAt = ISNULL(CreatedAt, SYSUTCDATETIME()), UpdatedAt = ISNULL(UpdatedAt, SYSUTCDATETIME());

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BlogPosts_CreatedByUserId' AND object_id = OBJECT_ID('dbo.BlogPosts'))
                CREATE INDEX IX_BlogPosts_CreatedByUserId ON dbo.BlogPosts (CreatedByUserId);

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BlogPosts_CreatedAt' AND object_id = OBJECT_ID('dbo.BlogPosts'))
                CREATE INDEX IX_BlogPosts_CreatedAt ON dbo.BlogPosts (CreatedAt DESC);

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Comments_BlogPostId' AND object_id = OBJECT_ID('dbo.Comments'))
                CREATE INDEX IX_Comments_BlogPostId ON dbo.Comments (BlogPostId);

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Comments_CreatedByUserId' AND object_id = OBJECT_ID('dbo.Comments'))
                CREATE INDEX IX_Comments_CreatedByUserId ON dbo.Comments (CreatedByUserId);

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Comments_CreatedAt' AND object_id = OBJECT_ID('dbo.Comments'))
                CREATE INDEX IX_Comments_CreatedAt ON dbo.Comments (CreatedAt DESC);
            """;

        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }
}