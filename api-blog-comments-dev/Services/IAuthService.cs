namespace api_blog_comments_dev.Services;

public interface IAuthService
{
    Task<string?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<RegisterUserResult> RegisterAsync(string username, string password, CancellationToken cancellationToken = default);
}