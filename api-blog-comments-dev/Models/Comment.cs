using System.ComponentModel.DataAnnotations;

namespace api_blog_comments_dev.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string Text { get; set; } = string.Empty;

        // foreign key
        public int BlogPostId { get; set; }
        public BlogPost? BlogPost { get; set; }
    }
}