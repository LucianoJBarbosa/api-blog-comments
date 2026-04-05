using System.ComponentModel.DataAnnotations;

namespace api_blog_comments_dev.DTOs
{
    /// <summary>
    /// Representa a credencial usada para autenticar um usuário e obter um JWT.
    /// </summary>
    public class LoginRequestDto
    {
        /// <summary>
        /// Nome de usuário utilizado no login.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Senha correspondente ao usuário informado.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Representa os dados necessários para cadastrar um novo usuário.
    /// </summary>
    public class RegisterRequestDto
    {
        /// <summary>
        /// Nome de usuário que será associado à conta.
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Senha inicial da conta.
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Retorna o token JWT gerado após autenticação bem-sucedida.
    /// </summary>
    public class AuthResponseDto
    {
        /// <summary>
        /// Token JWT a ser enviado no header Authorization com o prefixo Bearer.
        /// </summary>
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>
    /// Representa o usuário autenticado extraído do JWT atual.
    /// </summary>
    public class AuthenticatedUserProfileDto
    {
        /// <summary>
        /// Identificador interno do usuário.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome de usuário autenticado.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Papel autorizado do usuário.
        /// </summary>
        public string Role { get; set; } = string.Empty;
    }
}

