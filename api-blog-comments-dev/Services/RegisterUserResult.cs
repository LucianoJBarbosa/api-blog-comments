namespace api_blog_comments_dev.Services;

public sealed class RegisterUserResult
{
    public bool Succeeded { get; init; }
    public bool UsernameAlreadyExists { get; init; }
    public string? Token { get; init; }
}