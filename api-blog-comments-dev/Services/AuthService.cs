using api_blog_comments_dev.DTOs;
using api_blog_comments_dev.Repositories;

namespace api_blog_comments_dev.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUsersRepository _usersRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(IUsersRepository usersRepository, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService)
    {
        _usersRepository = usersRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<string?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await _usersRepository.GetByUsernameAsync(username, cancellationToken);
        if (user == null)
        {
            return null;
        }

        if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
        {
            return null;
        }

        return _jwtTokenService.GenerateToken(user.Id, user.Username, user.Role);
    }

    public async Task<RegisterUserResult> RegisterAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var passwordHash = _passwordHasher.HashPassword(password);
        AuthenticatedUserDto user;

        try
        {
            user = await _usersRepository.CreateAsync(username, passwordHash, UserRoles.Author, cancellationToken);
        }
        catch (UsernameAlreadyExistsException)
        {
            return new RegisterUserResult
            {
                Succeeded = false,
                UsernameAlreadyExists = true
            };
        }

        return new RegisterUserResult
        {
            Succeeded = true,
            Token = _jwtTokenService.GenerateToken(user.Id, user.Username, user.Role)
        };
    }
}