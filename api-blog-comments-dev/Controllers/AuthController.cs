using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using api_blog_comments_dev.Configuration;
using api_blog_comments_dev.DTOs;
using api_blog_comments_dev.Services;

namespace api_blog_comments_dev.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IJwtTokenService _jwtTokenService;
        private readonly JwtSettings _jwtSettings;

        public AuthController(IJwtTokenService jwtTokenService, IOptions<JwtSettings> jwtOptions)
        {
            _jwtTokenService = jwtTokenService;
            _jwtSettings = jwtOptions.Value;
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public ActionResult<AuthResponseDto> Login(LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Para este cenário, usamos um usuário fixo de exemplo.
            // Em produção, esta validação deve ser feita contra um repositório de usuários
            // (banco de dados, Identity, etc.).
            if (request.Username != "admin" || request.Password != "admin123")
            {
                return Unauthorized();
            }

            var token = _jwtTokenService.GenerateToken(request.Username);
            var response = new AuthResponseDto { Token = token };
            return Ok(response);
        }
    }
}

