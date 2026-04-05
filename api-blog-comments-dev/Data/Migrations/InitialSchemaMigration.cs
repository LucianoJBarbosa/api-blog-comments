using System.Data;
using Dapper;

namespace api_blog_comments_dev.Data.Migrations;

public sealed class InitialSchemaMigration : IDatabaseMigration
{
    private const string SqliteProvider = "sqlite";
    private const string SqlServerProvider = "sqlserver";

    public string Id => "0001_initial_schema";
    public string Description => "Creates the base schema for users, posts and comments.";

    public async Task ApplyAsync(IDbConnection connection, string provider, CancellationToken cancellationToken = default)
    {
        var sql = provider switch
        {
            SqliteProvider => """
                CREATE TABLE IF NOT EXISTS BlogPosts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    CreatedByUserId INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id)
                );

                CREATE TABLE IF NOT EXISTS Comments (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Text TEXT NOT NULL,
                    BlogPostId INTEGER NOT NULL,
                    CreatedByUserId INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id),
                    FOREIGN KEY (BlogPostId) REFERENCES BlogPosts(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL,
                    PasswordHash TEXT NOT NULL,
                    Role TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL
                );

                CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Username ON Users (Username);
                """,
            SqlServerProvider => """
                IF OBJECT_ID(N'dbo.BlogPosts', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.BlogPosts (
                        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        Title NVARCHAR(200) NOT NULL,
                        Content NVARCHAR(MAX) NOT NULL,
                        CreatedByUserId INT NOT NULL,
                        CreatedAt DATETIME2 NOT NULL,
                        UpdatedAt DATETIME2 NOT NULL,
                        CONSTRAINT FK_BlogPosts_Users
                            FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(Id)
                    );
                END;

                IF OBJECT_ID(N'dbo.Comments', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.Comments (
                        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        Text NVARCHAR(500) NOT NULL,
                        BlogPostId INT NOT NULL,
                        CreatedByUserId INT NOT NULL,
                        CreatedAt DATETIME2 NOT NULL,
                        UpdatedAt DATETIME2 NOT NULL,
                        CONSTRAINT FK_Comments_BlogPosts
                            FOREIGN KEY (BlogPostId) REFERENCES dbo.BlogPosts(Id) ON DELETE CASCADE,
                        CONSTRAINT FK_Comments_Users
                            FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(Id)
                    );
                END;

                IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.Users (
                        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        Username NVARCHAR(100) NOT NULL,
                        PasswordHash NVARCHAR(500) NOT NULL,
                        Role NVARCHAR(50) NOT NULL,
                        CreatedAt DATETIME2 NOT NULL,
                        UpdatedAt DATETIME2 NOT NULL,
                        CONSTRAINT UQ_Users_Username UNIQUE (Username)
                    );
                END;
                """,
            _ => throw new InvalidOperationException($"Database provider '{provider}' is not supported.")
        };

        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }
}