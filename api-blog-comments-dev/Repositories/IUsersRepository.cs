using api_blog_comments_dev.DTOs;

namespace api_blog_comments_dev.Repositories;

public interface IUsersRepository
{
    Task<AuthenticatedUserDto?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<AuthenticatedUserDto> CreateAsync(string username, string passwordHash, string role, CancellationToken cancellationToken = default);
}