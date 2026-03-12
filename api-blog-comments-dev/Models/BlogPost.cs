using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace api_blog_comments_dev.Models
{
    public class BlogPost
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}