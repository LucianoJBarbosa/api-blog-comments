using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using api_blog_comments_dev.Data;
using api_blog_comments_dev.Data.Migrations;
using api_blog_comments_dev.Services;

var exitCode = await RunAsync(args);
return exitCode;

static async Task<int> RunAsync(string[] args)
{
    var command = args.FirstOrDefault()?.Trim().ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(command) || command is "help" or "--help" or "-h")
    {
        PrintUsage();
        return 1;
    }

    var appProjectPath = ResolveAppProjectPath();
    IConfiguration configuration = new ConfigurationBuilder()
        .SetBasePath(appProjectPath)
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile("appsettings.Development.json", optional: true)
        .AddEnvironmentVariables()
        .Build();
    configuration = NormalizeLocalToolConfiguration(configuration, appProjectPath);

    var connectionFactory = new DbConnectionFactory(configuration);
    EnsureSqliteProvider(connectionFactory);

    var initializer = new DatabaseInitializer(connectionFactory);
    var passwordHasher = new Argon2IdPasswordHasher();

    try
    {
        switch (command)
        {
            case "migration-status":
                await PrintMigrationStatusAsync(configuration, initializer);
                return 0;
            case "reset-local-db":
                await ResetLocalDatabaseAsync(configuration, initializer);
                return 0;
            case "rebuild-demo-db":
                await ResetLocalDatabaseAsync(configuration, initializer);
                await SeedDemoDatabaseAsync(initializer, connectionFactory, passwordHasher);
                return 0;
            case "seed-demo-db":
                await SeedDemoDatabaseAsync(initializer, connectionFactory, passwordHasher);
                return 0;
            default:
                Console.Error.WriteLine($"Unknown command '{command}'.");
                PrintUsage();
                return 1;
        }
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine(exception.Message);
        return 1;
    }
}

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project tools/api-blog-comments-dev.Tools/api-blog-comments-dev.Tools.csproj -- migration-status");
    Console.WriteLine("  dotnet run --project tools/api-blog-comments-dev.Tools/api-blog-comments-dev.Tools.csproj -- reset-local-db");
    Console.WriteLine("  dotnet run --project tools/api-blog-comments-dev.Tools/api-blog-comments-dev.Tools.csproj -- rebuild-demo-db");
    Console.WriteLine("  dotnet run --project tools/api-blog-comments-dev.Tools/api-blog-comments-dev.Tools.csproj -- seed-demo-db");
}

static string ResolveAppProjectPath()
{
    return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../api-blog-comments-dev"));
}

static void EnsureSqliteProvider(IDbConnectionFactory connectionFactory)
{
    if (!string.Equals(connectionFactory.Provider, "sqlite", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("Local maintenance commands currently support only the Sqlite provider.");
    }
}

static async Task ResetLocalDatabaseAsync(IConfiguration configuration, DatabaseInitializer initializer)
{
    var databasePath = ResolveSqliteDatabasePath(configuration);

    DeleteIfExists(databasePath);
    DeleteIfExists(databasePath + "-wal");
    DeleteIfExists(databasePath + "-shm");

    await initializer.InitializeAsync();
    Console.WriteLine($"Local database reset: {databasePath}");
}

static async Task PrintMigrationStatusAsync(IConfiguration configuration, DatabaseInitializer initializer)
{
    var databasePath = TryResolveSqliteDatabasePath(configuration);
    DatabaseMigrationStatus status;

    if (databasePath is not null && !File.Exists(databasePath))
    {
        status = new DatabaseMigrationStatus("sqlite", initializer.GetKnownMigrations()
            .Select(ToPendingState)
            .ToArray());

        Console.WriteLine($"Database file not found: {databasePath}");
    }
    else
    {
        status = await initializer.GetStatusAsync();

        if (databasePath is not null)
        {
            Console.WriteLine($"Database file: {databasePath}");
        }
    }

    Console.WriteLine($"Provider: {status.Provider}");
    Console.WriteLine($"Applied migrations: {status.AppliedCount}");
    Console.WriteLine($"Pending migrations: {status.PendingCount}");
    Console.WriteLine("Migrations:");

    foreach (var migration in status.Migrations)
    {
        var marker = migration.IsApplied ? "applied" : "pending";
        var appliedAt = migration.AppliedAt is null ? string.Empty : $" at {migration.AppliedAt:O}";
        Console.WriteLine($"  - {migration.Id}: {marker}{appliedAt}");
        Console.WriteLine($"    {migration.Description}");
    }
}

static void DeleteIfExists(string path)
{
    if (File.Exists(path))
    {
        File.Delete(path);
    }
}

static string ResolveSqliteDatabasePath(IConfiguration configuration)
{
    return TryResolveSqliteDatabasePath(configuration)
        ?? throw new InvalidOperationException("The configured Sqlite Data Source is empty.");
}

static string? TryResolveSqliteDatabasePath(IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

    var sqliteConnectionStringBuilder = new SqliteConnectionStringBuilder(connectionString);
    if (string.IsNullOrWhiteSpace(sqliteConnectionStringBuilder.DataSource))
    {
        return null;
    }

    var databasePath = sqliteConnectionStringBuilder.DataSource;
    return databasePath;
}

static IConfiguration NormalizeLocalToolConfiguration(IConfiguration configuration, string appProjectPath)
{
    var provider = configuration["Database:Provider"]?.Trim().ToLowerInvariant();
    if (!string.Equals(provider, "sqlite", StringComparison.OrdinalIgnoreCase))
    {
        return configuration;
    }

    var connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

    var sqliteConnectionStringBuilder = new SqliteConnectionStringBuilder(connectionString);
    if (string.IsNullOrWhiteSpace(sqliteConnectionStringBuilder.DataSource) || Path.IsPathRooted(sqliteConnectionStringBuilder.DataSource))
    {
        return configuration;
    }

    sqliteConnectionStringBuilder.DataSource = Path.GetFullPath(Path.Combine(appProjectPath, sqliteConnectionStringBuilder.DataSource));

    return new ConfigurationBuilder()
        .AddConfiguration(configuration)
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = sqliteConnectionStringBuilder.ToString()
        })
        .Build();
}

static DatabaseMigrationState ToPendingState(IDatabaseMigration migration)
{
    return new DatabaseMigrationState(migration.Id, migration.Description, false, null);
}

static async Task SeedDemoDatabaseAsync(DatabaseInitializer initializer, IDbConnectionFactory connectionFactory, IPasswordHasher passwordHasher)
{
    await initializer.InitializeAsync();

    using var connection = await connectionFactory.CreateOpenConnectionAsync();
    var existingUsers = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Users;");
    if (existingUsers > 0)
    {
        throw new InvalidOperationException("The local database already contains data. Run reset-local-db before seeding demo content.");
    }

    using var transaction = connection.BeginTransaction();

    var adminId = await InsertUserAsync(connection, transaction, passwordHasher, "demo-admin", "DemoAdmin123!", UserRoles.Admin, DateTime.UtcNow.AddMinutes(-40));
    var authorId = await InsertUserAsync(connection, transaction, passwordHasher, "demo-author", "DemoAuthor123!", UserRoles.Author, DateTime.UtcNow.AddMinutes(-35));
    var authorTwoId = await InsertUserAsync(connection, transaction, passwordHasher, "demo-author-2", "DemoAuthorTwo123!", UserRoles.Author, DateTime.UtcNow.AddMinutes(-30));

    var firstPostId = await InsertPostAsync(
        connection,
        transaction,
        "Como a API foi estruturada",
        "Resumo da composicao da aplicacao, separacao entre controllers, services e repositories.",
        authorId,
        DateTime.UtcNow.AddMinutes(-25));

    var secondPostId = await InsertPostAsync(
        connection,
        transaction,
        "Observabilidade minima",
        "Health checks, correlation id e problem details enriquecido para facilitar diagnostico.",
        authorTwoId,
        DateTime.UtcNow.AddMinutes(-15));

    await InsertCommentAsync(connection, transaction, "Bom ponto para demonstrar o fluxo completo.", firstPostId, adminId, DateTime.UtcNow.AddMinutes(-20));
    await InsertCommentAsync(connection, transaction, "Esse post ajuda a explicar a arquitetura sem exagerar a complexidade.", firstPostId, authorTwoId, DateTime.UtcNow.AddMinutes(-18));
    await InsertCommentAsync(connection, transaction, "Esse endpoint de health ficou bom para demo e manutencao local.", secondPostId, authorId, DateTime.UtcNow.AddMinutes(-10));

    transaction.Commit();

    Console.WriteLine("Demo database seeded successfully.");
    Console.WriteLine("Users:");
    Console.WriteLine("  demo-admin / DemoAdmin123!");
    Console.WriteLine("  demo-author / DemoAuthor123!");
    Console.WriteLine("  demo-author-2 / DemoAuthorTwo123!");
}

static async Task<int> InsertUserAsync(
    System.Data.IDbConnection connection,
    System.Data.IDbTransaction transaction,
    IPasswordHasher passwordHasher,
    string username,
    string password,
    string role,
    DateTime timestamp)
{
    const string sql = """
        INSERT INTO Users (Username, PasswordHash, Role, CreatedAt, UpdatedAt)
        VALUES (@Username, @PasswordHash, @Role, @CreatedAt, @UpdatedAt);
        SELECT last_insert_rowid();
        """;

    return await connection.ExecuteScalarAsync<int>(sql, new
    {
        Username = username,
        PasswordHash = passwordHasher.HashPassword(password),
        Role = role,
        CreatedAt = timestamp,
        UpdatedAt = timestamp
    }, transaction);
}

static async Task<int> InsertPostAsync(
    System.Data.IDbConnection connection,
    System.Data.IDbTransaction transaction,
    string title,
    string content,
    int createdByUserId,
    DateTime timestamp)
{
    const string sql = """
        INSERT INTO BlogPosts (Title, Content, CreatedByUserId, CreatedAt, UpdatedAt)
        VALUES (@Title, @Content, @CreatedByUserId, @CreatedAt, @UpdatedAt);
        SELECT last_insert_rowid();
        """;

    return await connection.ExecuteScalarAsync<int>(sql, new
    {
        Title = title,
        Content = content,
        CreatedByUserId = createdByUserId,
        CreatedAt = timestamp,
        UpdatedAt = timestamp
    }, transaction);
}

static async Task InsertCommentAsync(
    System.Data.IDbConnection connection,
    System.Data.IDbTransaction transaction,
    string text,
    int blogPostId,
    int createdByUserId,
    DateTime timestamp)
{
    const string sql = """
        INSERT INTO Comments (Text, BlogPostId, CreatedByUserId, CreatedAt, UpdatedAt)
        VALUES (@Text, @BlogPostId, @CreatedByUserId, @CreatedAt, @UpdatedAt);
        """;

    await connection.ExecuteAsync(sql, new
    {
        Text = text,
        BlogPostId = blogPostId,
        CreatedByUserId = createdByUserId,
        CreatedAt = timestamp,
        UpdatedAt = timestamp
    }, transaction);
}