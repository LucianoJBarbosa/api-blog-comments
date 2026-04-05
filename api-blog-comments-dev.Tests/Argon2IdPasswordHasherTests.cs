using FluentAssertions;
using Xunit;
using api_blog_comments_dev.Services;

namespace api_blog_comments_dev.Tests;

public class Argon2IdPasswordHasherTests
{
    [Fact]
    public void HashPassword_ShouldGenerateVerifiableHash()
    {
        var hasher = new Argon2IdPasswordHasher();
        const string password = "senha-segura-123";

        var hash = hasher.HashPassword(password);

        hash.Should().NotBeNullOrWhiteSpace();
        hasher.VerifyPassword(password, hash).Should().BeTrue();
        hasher.VerifyPassword("senha-invalida", hash).Should().BeFalse();
    }
}