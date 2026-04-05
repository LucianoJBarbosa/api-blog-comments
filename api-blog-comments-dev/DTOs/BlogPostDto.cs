using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace api_blog_comments_dev.DTOs
{
    /// <summary>
    /// Representa um post completo com seus comentários.
    /// </summary>
    public class BlogPostDto
    {
        /// <summary>
        /// Identificador único do post.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Título do post.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Conteúdo principal do post.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Identificador do usuário que criou o post.
        /// </summary>
        public int CreatedByUserId { get; set; }

        /// <summary>
        /// Nome do usuário que criou o post.
        /// </summary>
        public string CreatedByUsername { get; set; } = string.Empty;

        /// <summary>
        /// Data de criação do post em UTC.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Data da última atualização do post em UTC.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Lista de comentários associados ao post.
        /// </summary>
        public List<CommentDto> Comments { get; set; } = new();
    }

    /// <summary>
    /// Dados necessários para criar ou atualizar um post.
    /// </summary>
    public class CreateBlogPostDto
    {
        /// <summary>
        /// Título do post com até 200 caracteres.
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Conteúdo textual do post.
        /// </summary>
        [Required]
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Representa a visão resumida de um post para listagens.
    /// </summary>
    public class BlogPostSummaryDto
    {
        /// <summary>
        /// Identificador único do post.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Título do post.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Identificador do usuário que criou o post.
        /// </summary>
        public int CreatedByUserId { get; set; }

        /// <summary>
        /// Nome do usuário que criou o post.
        /// </summary>
        public string CreatedByUsername { get; set; } = string.Empty;

        /// <summary>
        /// Data de criação do post em UTC.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Data da última atualização do post em UTC.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Quantidade de comentários vinculados ao post.
        /// </summary>
        public int CommentCount { get; set; }
    }
}