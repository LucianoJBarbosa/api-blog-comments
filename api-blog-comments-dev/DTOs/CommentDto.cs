using System.ComponentModel.DataAnnotations;

namespace api_blog_comments_dev.DTOs
{
    public class CommentDto
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public class CreateCommentDto
    {
        [Required]
        [StringLength(500)]
        public string Text { get; set; } = string.Empty;
    }
}