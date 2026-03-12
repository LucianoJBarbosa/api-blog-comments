using System.ComponentModel.DataAnnotations;

namespace api_blog_comments_dev.DTOs
{
    public class LoginRequestDto
    {
        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
    }
}

