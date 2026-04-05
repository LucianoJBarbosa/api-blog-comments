namespace api_blog_comments_dev.Data;

public sealed record DatabaseMigrationStatus(string Provider, IReadOnlyList<DatabaseMigrationState> Migrations)
{
    public int AppliedCount => Migrations.Count(static migration => migration.IsApplied);
    public int PendingCount => Migrations.Count - AppliedCount;
}

public sealed record DatabaseMigrationState(string Id, string Description, bool IsApplied, DateTime? AppliedAt);