using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using api_blog_comments_dev.DTOs;
using api_blog_comments_dev.Infrastructure;
using api_blog_comments_dev.Services;

namespace api_blog_comments_dev.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [EnableRateLimiting("AuthEndpoints")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // POST: api/auth/login
        /// <summary>
        /// Autentica o usuário e retorna um token JWT.
        /// </summary>
        /// <param name="request">Credenciais de autenticação.</param>
        /// <returns>Token JWT quando o login é válido.</returns>
        /// <response code="200">Autenticação realizada com sucesso.</response>
        /// <response code="400">A requisição contém dados inválidos.</response>
        /// <response code="401">Usuário ou senha inválidos.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var token = await _authService.AuthenticateAsync(request.Username, request.Password, HttpContext.RequestAborted);
            if (token == null)
            {
                return Unauthorized();
            }

            var response = new AuthResponseDto { Token = token };
            return Ok(response);
        }

        // POST: api/auth/register
        /// <summary>
        /// Cadastra um novo usuário e retorna um token JWT.
        /// </summary>
        /// <param name="request">Dados do usuário a ser criado.</param>
        /// <returns>Token JWT quando o cadastro é concluído.</returns>
        /// <response code="201">Usuário criado com sucesso.</response>
        /// <response code="400">A requisição contém dados inválidos.</response>
        /// <response code="409">O nome de usuário informado já está em uso.</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RegisterAsync(request.Username, request.Password, HttpContext.RequestAborted);
            if (result.UsernameAlreadyExists)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Username conflict",
                    Detail = "The informed username is already in use. Choose a different value and try again.",
                    Status = StatusCodes.Status409Conflict
                });
            }

            return StatusCode(StatusCodes.Status201Created, new AuthResponseDto
            {
                Token = result.Token ?? string.Empty
            });
        }

        // GET: api/auth/me
        /// <summary>
        /// Retorna os dados básicos do usuário autenticado no token atual.
        /// </summary>
        /// <returns>Perfil do usuário autenticado.</returns>
        /// <response code="200">Perfil retornado com sucesso.</response>
        /// <response code="401">O usuário não está autenticado.</response>
        [Authorize(Policy = AuthorizationPolicies.ApiUser)]
        [HttpGet("me")]
        [ProducesResponseType(typeof(AuthenticatedUserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult<AuthenticatedUserProfileDto> Me()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
            var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

            if (!int.TryParse(userIdValue, out var userId) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(role))
            {
                return Unauthorized();
            }

            return Ok(new AuthenticatedUserProfileDto
            {
                Id = userId,
                Username = username,
                Role = role
            });
        }
    }
}

