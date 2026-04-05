using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Xunit;
using api_blog_comments_dev.Data;

namespace api_blog_comments_dev.Tests;

public class DatabaseInitializerTests
{
    [Fact]
    public async Task GetStatusAsync_ShouldReport_AllMigrationsAsPending_WhenHistoryDoesNotExist()
    {
        var databasePath = CreateDatabasePath();
        await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
        {
            await connection.OpenAsync();
        }

        try
        {
            var initializer = CreateInitializer(databasePath);

            var status = await initializer.GetStatusAsync();

            status.Provider.Should().Be("sqlite");
            status.AppliedCount.Should().Be(0);
            status.PendingCount.Should().Be(2);
            status.Migrations.Should().OnlyContain(migration => !migration.IsApplied && migration.AppliedAt == null);
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task GetStatusAsync_ShouldReport_AllMigrationsAsApplied_AfterInitialization()
    {
        var databasePath = CreateDatabasePath();

        try
        {
            var initializer = CreateInitializer(databasePath);
            await initializer.InitializeAsync();

            var status = await initializer.GetStatusAsync();

            status.AppliedCount.Should().Be(2);
            status.PendingCount.Should().Be(0);
            status.Migrations.Should().OnlyContain(migration => migration.IsApplied && migration.AppliedAt.HasValue);
            status.Migrations.Select(migration => migration.Id)
                .Should()
                .ContainInOrder("0001_initial_schema", "0002_legacy_compatibility");
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    private static DatabaseInitializer CreateInitializer(string databasePath)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Sqlite",
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={databasePath}"
            })
            .Build();

        var connectionFactory = new DbConnectionFactory(configuration);
        return new DatabaseInitializer(connectionFactory);
    }

    private static string CreateDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), $"database-initializer-tests-{Guid.NewGuid():N}.db");
    }
}