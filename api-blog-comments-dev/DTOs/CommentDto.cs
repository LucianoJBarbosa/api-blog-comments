using System.ComponentModel.DataAnnotations;

namespace api_blog_comments_dev.DTOs
{
    /// <summary>
    /// Representa um comentário vinculado a um post.
    /// </summary>
    public class CommentDto
    {
        /// <summary>
        /// Identificador único do comentário.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Texto do comentário.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Identificador do usuário que criou o comentário.
        /// </summary>
        public int CreatedByUserId { get; set; }

        /// <summary>
        /// Nome do usuário que criou o comentário.
        /// </summary>
        public string CreatedByUsername { get; set; } = string.Empty;

        /// <summary>
        /// Data de criação do comentário em UTC.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Data da última atualização do comentário em UTC.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Dados necessários para criar ou atualizar um comentário.
    /// </summary>
    public class CreateCommentDto
    {
        /// <summary>
        /// Texto do comentário com até 500 caracteres.
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Text { get; set; } = string.Empty;
    }
}