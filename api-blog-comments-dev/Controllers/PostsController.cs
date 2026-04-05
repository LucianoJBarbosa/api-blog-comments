using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using api_blog_comments_dev.DTOs;
using api_blog_comments_dev.Infrastructure;
using api_blog_comments_dev.Repositories;
using api_blog_comments_dev.Services;

namespace api_blog_comments_dev.Controllers
{
    [ApiController]
    [Route("api/posts")]
    public class PostsController : ControllerBase
    {
        private readonly IPostsRepository _postsRepository;
        private CancellationToken RequestCancellationToken => HttpContext?.RequestAborted ?? CancellationToken.None;

        public PostsController(IPostsRepository postsRepository)
        {
            _postsRepository = postsRepository;
        }

        // GET: api/posts
        /// <summary>
        /// Lista todos os posts em formato resumido.
        /// </summary>
        /// <returns>Lista de posts com quantidade de comentários.</returns>
        /// <response code="200">Posts retornados com sucesso.</response>
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<BlogPostSummaryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResultDto<BlogPostSummaryDto>>> Get([FromQuery] PaginationQueryDto pagination)
        {
            var summaries = await _postsRepository.GetSummariesAsync(pagination, RequestCancellationToken);
            return Ok(summaries);
        }

        // POST: api/posts
        /// <summary>
        /// Cria um novo post.
        /// </summary>
        /// <param name="input">Dados do post a ser criado.</param>
        /// <returns>O post criado.</returns>
        /// <response code="201">Post criado com sucesso.</response>
        /// <response code="400">A requisição contém dados inválidos.</response>
        /// <response code="401">O usuário não está autenticado.</response>
        [Authorize(Policy = AuthorizationPolicies.AuthorOrAdmin)]
        [HttpPost]
        [ProducesResponseType(typeof(BlogPostDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<BlogPostDto>> Create(CreateBlogPostDto input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentUser(out var currentUser))
                return Unauthorized();

            var dto = await _postsRepository.CreateAsync(input, currentUser.UserId, RequestCancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }

        // GET: api/posts/{id}
        /// <summary>
        /// Obtém um post pelo identificador.
        /// </summary>
        /// <param name="id">Identificador do post.</param>
        /// <returns>O post completo com seus comentários.</returns>
        /// <response code="200">Post encontrado.</response>
        /// <response code="404">Nenhum post foi encontrado para o identificador informado.</response>
        [AllowAnonymous]
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(BlogPostDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BlogPostDto>> GetById(int id)
        {
            var post = await _postsRepository.GetByIdAsync(id, RequestCancellationToken);
            if (post == null)
                return NotFound();

            return Ok(post);
        }

        // PUT: api/posts/{id}
        /// <summary>
        /// Atualiza um post existente.
        /// </summary>
        /// <param name="id">Identificador do post.</param>
        /// <param name="input">Novos dados do post.</param>
        /// <returns>O post atualizado.</returns>
        /// <response code="200">Post atualizado com sucesso.</response>
        /// <response code="400">A requisição contém dados inválidos.</response>
        /// <response code="401">O usuário não está autenticado.</response>
        /// <response code="404">Nenhum post foi encontrado para o identificador informado.</response>
        [Authorize(Policy = AuthorizationPolicies.AuthorOrAdmin)]
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(BlogPostDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BlogPostDto>> Update(int id, CreateBlogPostDto input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentUser(out var currentUser))
                return Unauthorized();

            var existingPost = await _postsRepository.GetByIdAsync(id, RequestCancellationToken);
            if (existingPost == null)
                return NotFound();

            if (!currentUser.IsAdmin && existingPost.CreatedByUserId != currentUser.UserId)
                return Forbid();

            var post = await _postsRepository.UpdateAsync(id, input, RequestCancellationToken);
            if (post == null)
                return NotFound();

            return Ok(post);
        }

        // DELETE: api/posts/{id}
        /// <summary>
        /// Remove um post e seus comentários.
        /// </summary>
        /// <param name="id">Identificador do post.</param>
        /// <response code="204">Post removido com sucesso.</response>
        /// <response code="401">O usuário não está autenticado.</response>
        /// <response code="404">Nenhum post foi encontrado para o identificador informado.</response>
        [Authorize(Policy = AuthorizationPolicies.AuthorOrAdmin)]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            if (!TryGetCurrentUser(out var currentUser))
                return Unauthorized();

            var existingPost = await _postsRepository.GetByIdAsync(id, RequestCancellationToken);
            if (existingPost == null)
                return NotFound();

            if (!currentUser.IsAdmin && existingPost.CreatedByUserId != currentUser.UserId)
                return Forbid();

            var deleted = await _postsRepository.DeleteAsync(id, RequestCancellationToken);
            if (!deleted)
                return NotFound();

            return NoContent();
        }

        // GET: api/posts/{id}/comments
        /// <summary>
        /// Lista todos os comentários de um post.
        /// </summary>
        /// <param name="id">Identificador do post.</param>
        /// <param name="pagination">Parâmetros de paginação da listagem.</param>
        /// <returns>Lista de comentários do post.</returns>
        /// <response code="200">Comentários retornados com sucesso.</response>
        /// <response code="404">Nenhum post foi encontrado para o identificador informado.</response>
        [AllowAnonymous]
        [HttpGet("{id:int}/comments")]
        [ProducesResponseType(typeof(PagedResultDto<CommentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PagedResultDto<CommentDto>>> GetComments(int id, [FromQuery] PaginationQueryDto pagination)
        {
            var comments = await _postsRepository.GetCommentsAsync(id, pagination, RequestCancellationToken);
            if (comments == null)
                return NotFound();

            return Ok(comments);
        }

        // GET: api/posts/{id}/comments/{commentId}
        /// <summary>
        /// Obtém um comentário específico de um post.
        /// </summary>
        /// <param name="id">Identificador do post.</param>
        /// <param name="commentId">Identificador do comentário.</param>
        /// <returns>Comentário encontrado.</returns>
        /// <response code="200">Comentário encontrado.</response>
        /// <response code="404">Nenhum comentário foi encontrado para os identificadores informados.</response>
        [AllowAnonymous]
        [HttpGet("{id:int}/comments/{commentId:int}")]
        [ProducesResponseType(typeof(CommentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CommentDto>> GetCommentById(int id, int commentId)
        {
            var comment = await _postsRepository.GetCommentByIdAsync(id, commentId, RequestCancellationToken);

            if (comment == null)
                return NotFound();

            return Ok(comment);
        }

        // POST: api/posts/{id}/comments
        /// <summary>
        /// Adiciona um comentário a um post.
        /// </summary>
        /// <param name="id">Identificador do post.</param>
        /// <param name="input">Dados do comentário.</param>
        /// <returns>Comentário criado.</returns>
        /// <response code="201">Comentário criado com sucesso.</response>
        /// <response code="400">A requisição contém dados inválidos.</response>
        /// <response code="401">O usuário não está autenticado.</response>
        /// <response code="404">Nenhum post foi encontrado para o identificador informado.</response>
        [Authorize(Policy = AuthorizationPolicies.AuthorOrAdmin)]
        [HttpPost("{id:int}/comments")]
        [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CommentDto>> AddComment(int id, CreateCommentDto input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentUser(out var currentUser))
                return Unauthorized();

            var dto = await _postsRepository.AddCommentAsync(id, input, currentUser.UserId, RequestCancellationToken);
            if (dto == null)
                return NotFound();

            return CreatedAtAction(nameof(GetById), new { id }, dto);
        }

        // PUT: api/posts/{id}/comments/{commentId}
        /// <summary>
        /// Atualiza um comentário existente.
        /// </summary>
        /// <param name="id">Identificador do post.</param>
        /// <param name="commentId">Identificador do comentário.</param>
        /// <param name="input">Novos dados do comentário.</param>
        /// <returns>Comentário atualizado.</returns>
        /// <response code="200">Comentário atualizado com sucesso.</response>
        /// <response code="400">A requisição contém dados inválidos.</response>
        /// <response code="401">O usuário não está autenticado.</response>
        /// <response code="404">Nenhum comentário foi encontrado para os identificadores informados.</response>
        [Authorize(Policy = AuthorizationPolicies.AuthorOrAdmin)]
        [HttpPut("{id:int}/comments/{commentId:int}")]
        [ProducesResponseType(typeof(CommentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CommentDto>> UpdateComment(int id, int commentId, CreateCommentDto input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentUser(out var currentUser))
                return Unauthorized();

            var existingComment = await _postsRepository.GetCommentByIdAsync(id, commentId, RequestCancellationToken);
            if (existingComment == null)
                return NotFound();

            if (!currentUser.IsAdmin && existingComment.CreatedByUserId != currentUser.UserId)
                return Forbid();

            var comment = await _postsRepository.UpdateCommentAsync(id, commentId, input, RequestCancellationToken);
            if (comment == null)
                return NotFound();

            return Ok(comment);
        }

        // DELETE: api/posts/{id}/comments/{commentId}
        /// <summary>
        /// Remove um comentário de um post.
        /// </summary>
        /// <param name="id">Identificador do post.</param>
        /// <param name="commentId">Identificador do comentário.</param>
        /// <response code="204">Comentário removido com sucesso.</response>
        /// <response code="401">O usuário não está autenticado.</response>
        /// <response code="404">Nenhum comentário foi encontrado para os identificadores informados.</response>
        [Authorize(Policy = AuthorizationPolicies.AuthorOrAdmin)]
        [HttpDelete("{id:int}/comments/{commentId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteComment(int id, int commentId)
        {
            if (!TryGetCurrentUser(out var currentUser))
                return Unauthorized();

            var existingComment = await _postsRepository.GetCommentByIdAsync(id, commentId, RequestCancellationToken);
            if (existingComment == null)
                return NotFound();

            if (!currentUser.IsAdmin && existingComment.CreatedByUserId != currentUser.UserId)
                return Forbid();

            var deleted = await _postsRepository.DeleteCommentAsync(id, commentId, RequestCancellationToken);
            if (!deleted)
                return NotFound();

            return NoContent();
        }

        private bool TryGetCurrentUser(out CurrentUser currentUser)
        {
            currentUser = default;

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return false;
            }

            currentUser = new CurrentUser(userId, User.IsInRole(UserRoles.Admin));
            return true;
        }

        private readonly record struct CurrentUser(int UserId, bool IsAdmin);
    }
}