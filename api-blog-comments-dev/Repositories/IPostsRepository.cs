using api_blog_comments_dev.DTOs;

namespace api_blog_comments_dev.Repositories;

public interface IPostsRepository
{
    Task<PagedResultDto<BlogPostSummaryDto>> GetSummariesAsync(PaginationQueryDto pagination, CancellationToken cancellationToken = default);
    Task<BlogPostDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<BlogPostDto> CreateAsync(CreateBlogPostDto input, int createdByUserId, CancellationToken cancellationToken = default);
    Task<BlogPostDto?> UpdateAsync(int id, CreateBlogPostDto input, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResultDto<CommentDto>?> GetCommentsAsync(int postId, PaginationQueryDto pagination, CancellationToken cancellationToken = default);
    Task<CommentDto?> GetCommentByIdAsync(int postId, int commentId, CancellationToken cancellationToken = default);
    Task<CommentDto?> AddCommentAsync(int postId, CreateCommentDto input, int createdByUserId, CancellationToken cancellationToken = default);
    Task<CommentDto?> UpdateCommentAsync(int postId, int commentId, CreateCommentDto input, CancellationToken cancellationToken = default);
    Task<bool> DeleteCommentAsync(int postId, int commentId, CancellationToken cancellationToken = default);
}