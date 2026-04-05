using System.Data;
using Dapper;
using api_blog_comments_dev.Data;
using api_blog_comments_dev.DTOs;

namespace api_blog_comments_dev.Repositories;

public sealed class DapperPostsRepository : IPostsRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private const string SqliteProvider = "sqlite";
    private const string SqlServerProvider = "sqlserver";

    public DapperPostsRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResultDto<BlogPostSummaryDto>> GetSummariesAsync(PaginationQueryDto pagination, CancellationToken cancellationToken = default)
    {
        var sql = _connectionFactory.Provider switch
        {
            SqliteProvider => """
                SELECT COUNT(*)
                FROM BlogPosts;

                SELECT p.Id,
                       p.Title,
                       p.CreatedByUserId,
                       u.Username AS CreatedByUsername,
                       p.CreatedAt,
                       p.UpdatedAt,
                       COUNT(c.Id) AS CommentCount
                FROM BlogPosts p
                INNER JOIN Users u ON u.Id = p.CreatedByUserId
                LEFT JOIN Comments c ON c.BlogPostId = p.Id
                GROUP BY p.Id, p.Title, p.CreatedByUserId, u.Username, p.CreatedAt, p.UpdatedAt
                ORDER BY p.CreatedAt DESC, p.Id DESC
                LIMIT @PageSize OFFSET @Offset;
                """,
            SqlServerProvider => """
                SELECT COUNT(*)
                FROM dbo.BlogPosts;

                SELECT p.Id,
                       p.Title,
                       p.CreatedByUserId,
                       u.Username AS CreatedByUsername,
                       p.CreatedAt,
                       p.UpdatedAt,
                       COUNT(c.Id) AS CommentCount
                FROM dbo.BlogPosts p
                INNER JOIN dbo.Users u ON u.Id = p.CreatedByUserId
                LEFT JOIN dbo.Comments c ON c.BlogPostId = p.Id
                GROUP BY p.Id, p.Title, p.CreatedByUserId, u.Username, p.CreatedAt, p.UpdatedAt
                ORDER BY p.CreatedAt DESC, p.Id DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
                """,
            _ => throw new InvalidOperationException($"Database provider '{_connectionFactory.Provider}' is not supported.")
        };

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { pagination.PageSize, pagination.Offset }, cancellationToken: cancellationToken);
        using var grid = await connection.QueryMultipleAsync(command);
        var totalCount = await grid.ReadSingleAsync<int>();
        var items = (await grid.ReadAsync<BlogPostSummaryDto>()).ToList();

        return new PagedResultDto<BlogPostSummaryDto>
        {
            Items = items,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
        };
    }

    public async Task<BlogPostDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await GetByIdAsync(connection, id, cancellationToken);
    }

    public async Task<BlogPostDto> CreateAsync(CreateBlogPostDto input, int createdByUserId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var sql = _connectionFactory.Provider switch
        {
            SqliteProvider => """
                INSERT INTO BlogPosts (Title, Content, CreatedByUserId, CreatedAt, UpdatedAt)
                VALUES (@Title, @Content, @CreatedByUserId, @CreatedAt, @UpdatedAt);
                SELECT last_insert_rowid();
                """,
            SqlServerProvider => """
                INSERT INTO dbo.BlogPosts (Title, Content, CreatedByUserId, CreatedAt, UpdatedAt)
                OUTPUT INSERTED.Id
                VALUES (@Title, @Content, @CreatedByUserId, @CreatedAt, @UpdatedAt);
                """,
            _ => throw new InvalidOperationException($"Database provider '{_connectionFactory.Provider}' is not supported.")
        };

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new
        {
            input.Title,
            input.Content,
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken: cancellationToken);
        var id = await connection.ExecuteScalarAsync<int>(command);
        return (await GetByIdAsync(connection, id, cancellationToken))!;
    }

    public async Task<BlogPostDto?> UpdateAsync(int id, CreateBlogPostDto input, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE BlogPosts
            SET Title = @Title,
                Content = @Content,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { Id = id, input.Title, input.Content, UpdatedAt = DateTime.UtcNow }, cancellationToken: cancellationToken);
        var affectedRows = await connection.ExecuteAsync(command);

        return affectedRows == 0 ? null : await GetByIdAsync(connection, id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM BlogPosts WHERE Id = @Id;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        var affectedRows = await connection.ExecuteAsync(command);
        return affectedRows > 0;
    }

    public async Task<PagedResultDto<CommentDto>?> GetCommentsAsync(int postId, PaginationQueryDto pagination, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await PostExistsAsync(connection, postId, cancellationToken))
        {
            return null;
        }

        return await GetCommentsInternalAsync(connection, postId, pagination, cancellationToken);
    }

    public async Task<CommentDto?> GetCommentByIdAsync(int postId, int commentId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await GetCommentByIdAsync(connection, postId, commentId, cancellationToken);
    }

    public async Task<CommentDto?> AddCommentAsync(int postId, CreateCommentDto input, int createdByUserId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var sql = _connectionFactory.Provider switch
        {
            SqliteProvider => """
                INSERT INTO Comments (Text, BlogPostId, CreatedByUserId, CreatedAt, UpdatedAt)
                VALUES (@Text, @BlogPostId, @CreatedByUserId, @CreatedAt, @UpdatedAt);
                SELECT last_insert_rowid();
                """,
            SqlServerProvider => """
                INSERT INTO dbo.Comments (Text, BlogPostId, CreatedByUserId, CreatedAt, UpdatedAt)
                OUTPUT INSERTED.Id
                VALUES (@Text, @BlogPostId, @CreatedByUserId, @CreatedAt, @UpdatedAt);
                """,
            _ => throw new InvalidOperationException($"Database provider '{_connectionFactory.Provider}' is not supported.")
        };

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await PostExistsAsync(connection, postId, cancellationToken))
        {
            return null;
        }

        var command = new CommandDefinition(sql, new
        {
            input.Text,
            BlogPostId = postId,
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken: cancellationToken);
        var id = await connection.ExecuteScalarAsync<int>(command);
        return await GetCommentByIdAsync(connection, postId, id, cancellationToken);
    }

    public async Task<CommentDto?> UpdateCommentAsync(int postId, int commentId, CreateCommentDto input, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE Comments
            SET Text = @Text,
                UpdatedAt = @UpdatedAt
            WHERE BlogPostId = @PostId AND Id = @CommentId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { input.Text, PostId = postId, CommentId = commentId, UpdatedAt = DateTime.UtcNow }, cancellationToken: cancellationToken);
        var affectedRows = await connection.ExecuteAsync(command);
        return affectedRows == 0 ? null : await GetCommentByIdAsync(connection, postId, commentId, cancellationToken);
    }

    public async Task<bool> DeleteCommentAsync(int postId, int commentId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM Comments WHERE BlogPostId = @PostId AND Id = @CommentId;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { PostId = postId, CommentId = commentId }, cancellationToken: cancellationToken);
        var affectedRows = await connection.ExecuteAsync(command);
        return affectedRows > 0;
    }

    private static async Task<BlogPostDto?> GetByIdAsync(IDbConnection connection, int id, CancellationToken cancellationToken)
    {
        const string postSql = """
            SELECT p.Id,
                   p.Title,
                   p.Content,
                   p.CreatedByUserId,
                   u.Username AS CreatedByUsername,
                   p.CreatedAt,
                   p.UpdatedAt
            FROM BlogPosts p
            INNER JOIN Users u ON u.Id = p.CreatedByUserId
            WHERE p.Id = @Id;
            """;

        var postCommand = new CommandDefinition(postSql, new { Id = id }, cancellationToken: cancellationToken);
        var post = await connection.QuerySingleOrDefaultAsync<BlogPostRecord>(postCommand);
        if (post == null)
        {
            return null;
        }

        var comments = await GetAllCommentsInternalAsync(connection, id, cancellationToken);
        return new BlogPostDto
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            CreatedByUserId = post.CreatedByUserId,
            CreatedByUsername = post.CreatedByUsername,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            Comments = comments.ToList()
        };
    }

    private async Task<PagedResultDto<CommentDto>> GetCommentsInternalAsync(IDbConnection connection, int postId, PaginationQueryDto pagination, CancellationToken cancellationToken)
    {
        var sql = _connectionFactory.Provider switch
        {
            SqliteProvider => """
                SELECT COUNT(*)
                FROM Comments
                WHERE BlogPostId = @PostId;

                SELECT c.Id,
                       c.Text,
                       c.CreatedByUserId,
                       u.Username AS CreatedByUsername,
                       c.CreatedAt,
                       c.UpdatedAt
                FROM Comments c
                INNER JOIN Users u ON u.Id = c.CreatedByUserId
                WHERE c.BlogPostId = @PostId
                ORDER BY c.CreatedAt DESC, c.Id DESC
                LIMIT @PageSize OFFSET @Offset;
                """,
            SqlServerProvider => """
                SELECT COUNT(*)
                FROM dbo.Comments
                WHERE BlogPostId = @PostId;

                SELECT c.Id,
                       c.Text,
                       c.CreatedByUserId,
                       u.Username AS CreatedByUsername,
                       c.CreatedAt,
                       c.UpdatedAt
                FROM dbo.Comments c
                INNER JOIN dbo.Users u ON u.Id = c.CreatedByUserId
                WHERE c.BlogPostId = @PostId
                ORDER BY c.CreatedAt DESC, c.Id DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
                """,
            _ => throw new InvalidOperationException($"Database provider '{_connectionFactory.Provider}' is not supported.")
        };

        var command = new CommandDefinition(sql, new { PostId = postId, pagination.PageSize, pagination.Offset }, cancellationToken: cancellationToken);
        using var grid = await connection.QueryMultipleAsync(command);
        var totalCount = await grid.ReadSingleAsync<int>();
        var items = (await grid.ReadAsync<CommentDto>()).ToList();

        return new PagedResultDto<CommentDto>
        {
            Items = items,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
        };
    }

    private static async Task<List<CommentDto>> GetAllCommentsInternalAsync(IDbConnection connection, int postId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT c.Id,
                   c.Text,
                   c.CreatedByUserId,
                   u.Username AS CreatedByUsername,
                   c.CreatedAt,
                   c.UpdatedAt
            FROM Comments c
            INNER JOIN Users u ON u.Id = c.CreatedByUserId
            WHERE c.BlogPostId = @PostId
            ORDER BY c.CreatedAt DESC, c.Id DESC;
            """;

        var command = new CommandDefinition(sql, new { PostId = postId }, cancellationToken: cancellationToken);
        var comments = await connection.QueryAsync<CommentDto>(command);
        return comments.ToList();
    }

    private static async Task<CommentDto?> GetCommentByIdAsync(IDbConnection connection, int postId, int commentId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT c.Id,
                   c.Text,
                   c.CreatedByUserId,
                   u.Username AS CreatedByUsername,
                   c.CreatedAt,
                   c.UpdatedAt
            FROM Comments c
            INNER JOIN Users u ON u.Id = c.CreatedByUserId
            WHERE c.BlogPostId = @PostId AND c.Id = @CommentId;
            """;

        var command = new CommandDefinition(sql, new { PostId = postId, CommentId = commentId }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<CommentDto>(command);
    }

    private static async Task<bool> PostExistsAsync(IDbConnection connection, int postId, CancellationToken cancellationToken)
    {
        var sql = connection.GetType().Name.Contains("Sqlite", StringComparison.OrdinalIgnoreCase)
            ? "SELECT 1 FROM BlogPosts WHERE Id = @Id LIMIT 1;"
            : "SELECT TOP (1) 1 FROM dbo.BlogPosts WHERE Id = @Id;";
        var command = new CommandDefinition(sql, new { Id = postId }, cancellationToken: cancellationToken);
        var exists = await connection.ExecuteScalarAsync<long?>(command);
        return exists.HasValue;
    }

    private sealed class BlogPostRecord
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public int CreatedByUserId { get; init; }
        public string CreatedByUsername { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}