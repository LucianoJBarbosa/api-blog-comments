using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using api_blog_comments_dev.Services;

namespace api_blog_comments_dev.Infrastructure;

public static class AuthorizationPolicies
{
    public const string ApiUser = nameof(ApiUser);
    public const string AuthorOrAdmin = nameof(AuthorOrAdmin);

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(ApiUser, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireClaim(ClaimTypes.NameIdentifier);
        });

        options.AddPolicy(AuthorOrAdmin, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireClaim(ClaimTypes.NameIdentifier);
            policy.RequireRole(UserRoles.Admin, UserRoles.Author);
        });
    }
}