using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using api_blog_comments_dev.Data;
using api_blog_comments_dev.DTOs;

namespace api_blog_comments_dev.Repositories;

public sealed class DapperUsersRepository : IUsersRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private const string SqliteProvider = "sqlite";
    private const string SqlServerProvider = "sqlserver";

    public DapperUsersRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AuthenticatedUserDto?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, Username, PasswordHash, Role, CreatedAt, UpdatedAt
            FROM Users
            WHERE Username = @Username;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { Username = username }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AuthenticatedUserDto>(command);
    }

    public async Task<AuthenticatedUserDto> CreateAsync(string username, string passwordHash, string role, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var sql = _connectionFactory.Provider switch
        {
            SqliteProvider => """
                INSERT INTO Users (Username, PasswordHash, Role, CreatedAt, UpdatedAt)
                VALUES (@Username, @PasswordHash, @Role, @CreatedAt, @UpdatedAt);

                SELECT last_insert_rowid();
                """,
            SqlServerProvider => """
                INSERT INTO dbo.Users (Username, PasswordHash, Role, CreatedAt, UpdatedAt)
                OUTPUT INSERTED.Id
                VALUES (@Username, @PasswordHash, @Role, @CreatedAt, @UpdatedAt);
                """,
            _ => throw new InvalidOperationException($"Database provider '{_connectionFactory.Provider}' is not supported.")
        };

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new { Username = username, PasswordHash = passwordHash, Role = role, CreatedAt = now, UpdatedAt = now },
            cancellationToken: cancellationToken);

        int id;

        try
        {
            id = await connection.ExecuteScalarAsync<int>(command);
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            throw new UsernameAlreadyExistsException(username);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            throw new UsernameAlreadyExistsException(username);
        }

        return new AuthenticatedUserDto
        {
            Id = id,
            Username = username,
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}