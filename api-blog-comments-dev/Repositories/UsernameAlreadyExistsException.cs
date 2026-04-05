namespace api_blog_comments_dev.Repositories;

public sealed class UsernameAlreadyExistsException(string username) : Exception($"Username '{username}' is already in use.")
{
    public string Username { get; } = username;
}