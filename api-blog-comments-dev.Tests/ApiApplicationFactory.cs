using System;
using System.Collections.Generic;
using System.IO;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using api_blog_comments_dev.Data;
using api_blog_comments_dev.Services;

namespace api_blog_comments_dev.Tests;

public sealed class ApiApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"api-blog-comments-tests-{Guid.NewGuid():N}.db");
    private readonly string? _previousAuthRateLimitingEnabled;
    private readonly string? _previousAuthRateLimitingPermitLimit;
    private readonly string? _previousAuthRateLimitingWindowSeconds;
    private readonly string? _previousJwtKey;

    public ApiApplicationFactory()
    {
        _previousAuthRateLimitingEnabled = Environment.GetEnvironmentVariable("AuthRateLimiting__Enabled");
        _previousAuthRateLimitingPermitLimit = Environment.GetEnvironmentVariable("AuthRateLimiting__PermitLimit");
        _previousAuthRateLimitingWindowSeconds = Environment.GetEnvironmentVariable("AuthRateLimiting__WindowSeconds");
        _previousJwtKey = Environment.GetEnvironmentVariable("Jwt__Key");

        Environment.SetEnvironmentVariable("AuthRateLimiting__Enabled", "false");
        Environment.SetEnvironmentVariable("AuthRateLimiting__PermitLimit", "1000");
        Environment.SetEnvironmentVariable("AuthRateLimiting__WindowSeconds", "60");
        Environment.SetEnvironmentVariable("Jwt__Key", "tests-only-jwt-secret-key-with-32chars-min");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Sqlite",
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_databasePath}"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbConnectionFactory>();
            services.RemoveAll<DatabaseInitializer>();

            services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
            services.AddSingleton<DatabaseInitializer>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Environment.SetEnvironmentVariable("AuthRateLimiting__Enabled", _previousAuthRateLimitingEnabled);
            Environment.SetEnvironmentVariable("AuthRateLimiting__PermitLimit", _previousAuthRateLimitingPermitLimit);
            Environment.SetEnvironmentVariable("AuthRateLimiting__WindowSeconds", _previousAuthRateLimitingWindowSeconds);
            Environment.SetEnvironmentVariable("Jwt__Key", _previousJwtKey);
        }

        base.Dispose(disposing);
    }

    public void ResetDatabase()
    {
        using var scope = Services.CreateScope();
        var databaseInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        databaseInitializer.InitializeAsync().GetAwaiter().GetResult();

        var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        using var connection = connectionFactory.CreateOpenConnectionAsync().GetAwaiter().GetResult();
        connection.Execute(
            """
            DELETE FROM Comments;
            DELETE FROM BlogPosts;
            DELETE FROM Users;
            DELETE FROM sqlite_sequence WHERE name IN ('BlogPosts', 'Comments', 'Users');
            """);

        databaseInitializer.InitializeAsync().GetAwaiter().GetResult();
    }

    public void PromoteUserToAdmin(string username)
    {
        using var scope = Services.CreateScope();
        var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        using var connection = connectionFactory.CreateOpenConnectionAsync().GetAwaiter().GetResult();

        var sql = connectionFactory.Provider switch
        {
            "sqlite" => "UPDATE Users SET Role = @Role WHERE Username = @Username;",
            "sqlserver" => "UPDATE dbo.Users SET Role = @Role WHERE Username = @Username;",
            _ => throw new InvalidOperationException($"Database provider '{connectionFactory.Provider}' is not supported.")
        };

        connection.Execute(sql, new { Username = username, Role = UserRoles.Admin });
    }
}