using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api_blog_comments_dev.Data;
using api_blog_comments_dev.DTOs;
using api_blog_comments_dev.Models;

namespace api_blog_comments_dev.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public PostsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: api/posts
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BlogPostSummaryDto>>> Get()
        {
            var summaries = await _db.BlogPosts
                .Select(p => new BlogPostSummaryDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    CommentCount = p.Comments.Count
                })
                .ToListAsync();

            return Ok(summaries);
        }

        // POST: api/posts
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<BlogPostDto>> Create(CreateBlogPostDto input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var post = new BlogPost
            {
                Title = input.Title,
                Content = input.Content
            };
            _db.BlogPosts.Add(post);
            await _db.SaveChangesAsync();

            var dto = new BlogPostDto
            {
                Id = post.Id,
                Title = post.Title,
                Content = post.Content,
                Comments = new List<CommentDto>()
            };

            return CreatedAtAction(nameof(GetById), new { id = post.Id }, dto);
        }

        // GET: api/posts/{id}
        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public async Task<ActionResult<BlogPostDto>> GetById(int id)
        {
            var post = await _db.BlogPosts
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
                return NotFound();

            var dto = new BlogPostDto
            {
                Id = post.Id,
                Title = post.Title,
                Content = post.Content,
                Comments = post.Comments.Select(c => new CommentDto { Id = c.Id, Text = c.Text }).ToList()
            };

            return Ok(dto);
        }

        // POST: api/posts/{id}/comments
        [Authorize]
        [HttpPost("{id:int}/comments")]
        public async Task<ActionResult<CommentDto>> AddComment(int id, CreateCommentDto input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var post = await _db.BlogPosts.FindAsync(id);
            if (post == null)
                return NotFound();

            var comment = new Comment { Text = input.Text, BlogPostId = id };
            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();

            var dto = new CommentDto { Id = comment.Id, Text = comment.Text };
            return CreatedAtAction(nameof(GetById), new { id }, dto);
        }
    }
}