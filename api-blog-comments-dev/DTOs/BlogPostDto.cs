using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace api_blog_comments_dev.DTOs
{
    public class BlogPostDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public List<CommentDto> Comments { get; set; } = new();
    }

    public class CreateBlogPostDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;
    }

    public class BlogPostSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int CommentCount { get; set; }
    }
}