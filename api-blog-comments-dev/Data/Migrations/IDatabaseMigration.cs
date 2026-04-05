using System.Data;

namespace api_blog_comments_dev.Data.Migrations;

public interface IDatabaseMigration
{
    string Id { get; }
    string Description { get; }
    Task ApplyAsync(IDbConnection connection, string provider, CancellationToken cancellationToken = default);
}