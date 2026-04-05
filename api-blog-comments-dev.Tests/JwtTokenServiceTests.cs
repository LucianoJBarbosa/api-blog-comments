using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using api_blog_comments_dev.Configuration;
using api_blog_comments_dev.Services;

namespace api_blog_comments_dev.Tests;

public class JwtTokenServiceTests
{
    [Fact]
    public void GenerateToken_ShouldReturn_ValidJwt()
    {
        // Arrange
        var settings = new JwtSettings
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Key = "TEST_SECRET_KEY_FOR_UNIT_TESTS_123456",
            ExpirationMinutes = 60
        };

        var options = Options.Create(settings);
        var service = new JwtTokenService(options);

        // Act
        var tokenString = service.GenerateToken(42, "test-user", UserRoles.Author);

        // Assert
        tokenString.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        token.Issuer.Should().Be(settings.Issuer);
        token.Audiences.Should().Contain(settings.Audience);
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "42");
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == "test-user");
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == UserRoles.Author);
    }
}

