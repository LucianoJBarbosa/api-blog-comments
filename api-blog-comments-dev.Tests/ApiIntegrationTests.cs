using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using api_blog_comments_dev.DTOs;
using api_blog_comments_dev.Services;

namespace api_blog_comments_dev.Tests;

[Collection("ApiIntegrationTests")]
public class ApiIntegrationTests : IClassFixture<ApiApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ApiApplicationFactory _factory;

    public ApiIntegrationTests(ApiApplicationFactory factory)
    {
        _factory = factory;
        factory.ResetDatabase();
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task GetPosts_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/posts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var posts = await response.Content.ReadFromJsonAsync<PagedResultDto<BlogPostSummaryDto>>();
        posts.Should().NotBeNull();
        posts!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Request_ShouldReturnCorrelationIdHeader()
    {
        const string correlationId = "integration-test-correlation-id";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-ID", correlationId);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().Contain(header => header.Key == "X-Correlation-ID");
        response.Headers.GetValues("X-Correlation-ID").Should().ContainSingle().Which.Should().Be(correlationId);
    }

    [Fact]
    public async Task ValidationProblemDetails_ShouldExposeTraceAndCorrelationIds()
    {
        const string correlationId = "validation-problem-correlation-id";
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register")
        {
            Content = JsonContent.Create(new RegisterRequestDto
            {
                Username = "ab",
                Password = "123"
            })
        };
        request.Headers.Add("X-Correlation-ID", correlationId);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.GetValues("X-Correlation-ID").Should().ContainSingle().Which.Should().Be(correlationId);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().NotBeNull();
        if (body is null)
        {
            throw new InvalidOperationException("Expected validation problem details body.");
        }

        body.Should().ContainKey("traceId");
        body["traceId"].Should().NotBeNull();
        body.Should().ContainKey("correlationId");
        body["correlationId"]!.ToString().Should().Be(correlationId);
    }

    [Fact]
    public async Task HealthEndpoints_ShouldReturnOk()
    {
        var livenessResponse = await _client.GetAsync("/health/live");
        var readinessResponse = await _client.GetAsync("/health/ready");
        var healthResponse = await _client.GetAsync("/health");

        livenessResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        readinessResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        healthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_ShouldReturnToken_WhenCredentialsAreValid()
    {
        // Arrange
        var registeredUser = await RegisterAsync("login-user", "senha-segura-123");
        var payload = new LoginRequestDto
        {
            Username = registeredUser.Username,
            Password = registeredUser.Password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Register_ShouldCreateUser_AndReturnToken()
    {
        var payload = new RegisterRequestDto
        {
            Username = "luciano",
            Password = "senha-segura-123"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Register_ShouldReturnConflict_WhenUsernameAlreadyExists()
    {
        await RegisterAsync("usuario-existente", "senha-segura-123");

        var payload = new RegisterRequestDto
        {
            Username = "usuario-existente",
            Password = "senha-segura-123"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task RegisterToken_ShouldAllowAccess_ToProtectedEndpoint()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Username = "novo-autor",
            Password = "senha-segura-123"
        });

        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var createResponse = await _client.PostAsJsonAsync("/api/posts", new CreateBlogPostDto
        {
            Title = "Post do novo usuario",
            Content = "Conteudo criado com token do cadastro"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Me_ShouldReturnAuthenticatedUserProfile()
    {
        var token = await RegisterAndGetJwtAsync("perfil-user", "senha-segura-123");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthenticatedUserProfileDto>();
        body.Should().NotBeNull();
        body!.Username.Should().Be("perfil-user");
        body.Role.Should().Be(UserRoles.Author);
        body.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Me_ShouldReturnUnauthorized_WhenTokenIsMissing()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task NonAdmin_ShouldNotUpdate_PostCreatedByAnotherUser()
    {
        var authorToken = await RegisterAndGetJwtAsync("autor-a", "senha-segura-123");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authorToken);

        var createResponse = await _client.PostAsJsonAsync("/api/posts", new CreateBlogPostDto
        {
            Title = "Post do autor A",
            Content = "Conteudo A"
        });

        var post = await createResponse.Content.ReadFromJsonAsync<BlogPostDto>();

        var otherAuthorToken = await RegisterAndGetJwtAsync("autor-b", "senha-segura-123");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherAuthorToken);

        var updateResponse = await _client.PutAsJsonAsync($"/api/posts/{post!.Id}", new CreateBlogPostDto
        {
            Title = "Tentativa indevida",
            Content = "Nao deveria atualizar"
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_ShouldBeAbleToUpdate_PostCreatedByAnotherUser()
    {
        var authorToken = await RegisterAndGetJwtAsync("autor-c", "senha-segura-123");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authorToken);

        var createResponse = await _client.PostAsJsonAsync("/api/posts", new CreateBlogPostDto
        {
            Title = "Post do autor C",
            Content = "Conteudo C"
        });

        var post = await createResponse.Content.ReadFromJsonAsync<BlogPostDto>();

        var adminToken = await RegisterAndGetJwtAsync("admin-real", "senha-segura-123", UserRoles.Admin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var updateResponse = await _client.PutAsJsonAsync($"/api/posts/{post!.Id}", new CreateBlogPostDto
        {
            Title = "Atualizado pelo admin",
            Content = "Admin pode editar"
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NonAdmin_ShouldNotDelete_CommentCreatedByAnotherUser()
    {
        var authorToken = await RegisterAndGetJwtAsync("autor-d", "senha-segura-123");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authorToken);

        var createPostResponse = await _client.PostAsJsonAsync("/api/posts", new CreateBlogPostDto
        {
            Title = "Post base ownership",
            Content = "Conteudo"
        });
        var post = await createPostResponse.Content.ReadFromJsonAsync<BlogPostDto>();

        var createCommentResponse = await _client.PostAsJsonAsync($"/api/posts/{post!.Id}/comments", new CreateCommentDto
        {
            Text = "Comentario do autor D"
        });
        var comment = await createCommentResponse.Content.ReadFromJsonAsync<CommentDto>();

        var otherAuthorToken = await RegisterAndGetJwtAsync("autor-e", "senha-segura-123");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherAuthorToken);

        var deleteResponse = await _client.DeleteAsync($"/api/posts/{post.Id}/comments/{comment!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OpenApiEndpoint_ShouldReturnDocument()
    {
        // Act
        var response = await _client.GetAsync("/docs/openapi/v1.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var content = await response.Content.ReadAsStringAsync();
        var compactContent = string.Concat(content.Where(c => !char.IsWhiteSpace(c)));

        content.Should().Contain("\"openapi\"");
        content.Should().Contain("\"securitySchemes\"");
        content.Should().Contain("\"Bearer\"");
        compactContent.Should().Contain("\"security\":[{\"Bearer\":[]}]", because: "o documento runtime deve marcar os endpoints protegidos com o esquema Bearer");
        content.Should().NotContain("\"#/components/securitySchemes/Bearer\"");
        content.Should().MatchRegex("\"/api/(Posts|posts)\"");
        content.Should().Contain("/api/auth/me");
    }

    [Fact]
    public async Task ScalarEndpoint_ShouldReturnHtmlPage()
    {
        // Act
        var response = await _client.GetAsync("/docs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("<title>API Blog Comments</title>");
        content.Should().Contain("scalar.js");
        content.Should().Contain("scalar.aspnetcore.js");
        content.Should().Contain("docs/openapi/v1.json");
        content.Should().Contain("Bearer");
    }

    [Fact]
    public async Task ScalarConfigScript_ShouldNotBeExposed()
    {
        // Act
        var response = await _client.GetAsync("/scalar-config.js");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
    {
        // Arrange
        await RegisterAsync("login-invalido", "senha-segura-123");
        var payload = new LoginRequestDto
        {
            Username = "login-invalido",
            Password = "senha-invalida"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreatePost_ShouldReturnUnauthorized_WhenTokenIsMissing()
    {
        // Arrange
        var payload = new CreateBlogPostDto
        {
            Title = "Post sem token",
            Content = "Conteudo"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/posts", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreatePost_ShouldReturnUnauthorized_WhenTokenIsInvalid()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token-invalido");

        // Act
        var response = await _client.PostAsJsonAsync("/api/posts", new CreateBlogPostDto
        {
            Title = "Post com token invalido",
            Content = "Conteudo"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreatePost_ShouldReturnBadRequest_WhenPayloadIsInvalid()
    {
        // Arrange
        var token = await RegisterAndGetJwtAsync("autor-badrequest", "senha-segura-123");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync("/api/posts", new CreateBlogPostDto
        {
            Title = string.Empty,
            Content = string.Empty
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePost_ShouldReturnNotFound_WhenPostDoesNotExist()
    {
        // Arrange
        var token = await RegisterAndGetJwtAsync("autor-notfound-update", "senha-segura-123");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PutAsJsonAsync("/api/posts/999", new CreateBlogPostDto
        {
            Title = "Nao existe",
            Content = "Nao existe"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCommentById_ShouldReturnNotFound_WhenCommentDoesNotExist()
    {
        // Arrange
        var token = await RegisterAndGetJwtAsync("autor-comment-notfound", "senha-segura-123");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createPostResponse = await _client.PostAsJsonAsync("/api/posts", new CreateBlogPostDto
        {
            Title = "Post base",
            Content = "Conteudo base"
        });
        var createdPost = await createPostResponse.Content.ReadFromJsonAsync<BlogPostDto>();

        // Act
        var response = await _client.GetAsync($"/api/posts/{createdPost!.Id}/comments/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FullCrudFlow_ShouldCreateUpdateAndDeletePostAndComment()
    {
        // Arrange
        var token = await RegisterAndGetJwtAsync("autor-crud", "senha-segura-123");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create post
        var createPostResponse = await _client.PostAsJsonAsync("/api/posts", new CreateBlogPostDto
        {
            Title = "Primeiro post",
            Content = "Conteudo inicial"
        });

        createPostResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdPost = await createPostResponse.Content.ReadFromJsonAsync<BlogPostDto>();
        createdPost.Should().NotBeNull();

        // Update post
        var updatePostResponse = await _client.PutAsJsonAsync($"/api/posts/{createdPost!.Id}", new CreateBlogPostDto
        {
            Title = "Primeiro post atualizado",
            Content = "Conteudo atualizado"
        });

        updatePostResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedPost = await updatePostResponse.Content.ReadFromJsonAsync<BlogPostDto>();
        updatedPost!.Title.Should().Be("Primeiro post atualizado");

        // Create comment
        var createCommentResponse = await _client.PostAsJsonAsync($"/api/posts/{createdPost.Id}/comments", new CreateCommentDto
        {
            Text = "Primeiro comentario"
        });

        createCommentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdComment = await createCommentResponse.Content.ReadFromJsonAsync<CommentDto>();
        createdComment.Should().NotBeNull();

        // Update comment
        var updateCommentResponse = await _client.PutAsJsonAsync($"/api/posts/{createdPost.Id}/comments/{createdComment!.Id}", new CreateCommentDto
        {
            Text = "Comentario atualizado"
        });

        updateCommentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedComment = await updateCommentResponse.Content.ReadFromJsonAsync<CommentDto>();
        updatedComment!.Text.Should().Be("Comentario atualizado");

        // Read comments
        var getCommentsResponse = await _client.GetAsync($"/api/posts/{createdPost.Id}/comments");
        getCommentsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var comments = await getCommentsResponse.Content.ReadFromJsonAsync<PagedResultDto<CommentDto>>();
        comments.Should().NotBeNull();
        comments!.Items.Should().ContainSingle(c => c.Id == createdComment.Id && c.Text == "Comentario atualizado");

        var healthResponse = await _client.GetAsync("/health");
        healthResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Delete comment
        var deleteCommentResponse = await _client.DeleteAsync($"/api/posts/{createdPost.Id}/comments/{createdComment.Id}");
        deleteCommentResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Delete post
        var deletePostResponse = await _client.DeleteAsync($"/api/posts/{createdPost.Id}");
        deletePostResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify post was deleted
        var getDeletedPostResponse = await _client.GetAsync($"/api/posts/{createdPost.Id}");
        getDeletedPostResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<(string Username, string Password)> RegisterAsync(string username, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Username = username,
            Password = password
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (username, password);
    }

    private async Task<string> RegisterAndGetJwtAsync(string username, string password)
    {
        return await RegisterAndGetJwtAsync(username, password, null);
    }

    private async Task<string> RegisterAndGetJwtAsync(string username, string password, string? role)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Username = username,
            Password = password
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        if (role == UserRoles.Admin)
        {
            _factory.PromoteUserToAdmin(username);

            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
            {
                Username = username,
                Password = password
            });

            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var loginBody = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            loginBody.Should().NotBeNull();
            return loginBody!.Token;
        }

        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        body.Should().NotBeNull();
        return body!.Token;
    }
}