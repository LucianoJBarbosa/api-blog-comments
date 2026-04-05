using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Xunit;
using api_blog_comments_dev.Controllers;
using api_blog_comments_dev.Data;
using api_blog_comments_dev.DTOs;
using api_blog_comments_dev.Repositories;
using api_blog_comments_dev.Services;

namespace api_blog_comments_dev.Tests;

public class PostsControllerTests
{
    private static (IPostsRepository Repository, string DatabasePath) CreateRepository()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"posts-controller-tests-{Guid.NewGuid():N}.db");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Sqlite",
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={databasePath}"
            })
            .Build();

        var connectionFactory = new DbConnectionFactory(configuration);
        var databaseInitializer = new DatabaseInitializer(connectionFactory);
        databaseInitializer.InitializeAsync().GetAwaiter().GetResult();
        var passwordHasher = new Argon2IdPasswordHasher();
        var usersRepository = new DapperUsersRepository(connectionFactory);
        usersRepository.CreateAsync("test-user", passwordHasher.HashPassword("senha-segura-123"), UserRoles.Author).GetAwaiter().GetResult();

        return (new DapperPostsRepository(connectionFactory), databasePath);
    }

    private static PostsController CreateController(IPostsRepository repository, int userId = 1, string role = UserRoles.Author)
    {
        var controller = new PostsController(repository)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                        new Claim(ClaimTypes.Name, "test-user"),
                        new Claim(ClaimTypes.Role, role),
                    ],
                    authenticationType: "TestAuth"))
                }
            }
        };

        return controller;
    }
    [Fact]
    public async Task Get_ShouldReturn_ListOfSummaries()
    {
        // Arrange
        var (repository, databasePath) = CreateRepository();
        await repository.CreateAsync(new CreateBlogPostDto { Title = "Post 1", Content = "Content 1" }, 1);
        await repository.CreateAsync(new CreateBlogPostDto { Title = "Post 2", Content = "Content 2" }, 1);

        try
        {
            var controller = CreateController(repository);

            // Act
            var result = await controller.Get(new PaginationQueryDto());

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();

            var summaries = okResult!.Value as PagedResultDto<BlogPostSummaryDto>;
            summaries.Should().NotBeNull();
            summaries!.Items.Should().HaveCount(2);
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Create_ShouldPersist_Post_WhenModelIsValid()
    {
        // Arrange
        var (repository, databasePath) = CreateRepository();
        var controller = CreateController(repository);

        var input = new CreateBlogPostDto
        {
            Title = "New Post",
            Content = "Post content"
        };

        try
        {
            // Act
            var result = await controller.Create(input);

            // Assert
            var createdResult = result.Result as CreatedAtActionResult;
            createdResult.Should().NotBeNull();

            var dto = createdResult!.Value as BlogPostDto;
            dto.Should().NotBeNull();
            dto!.Title.Should().Be(input.Title);

            var summaries = await repository.GetSummariesAsync(new PaginationQueryDto());
            summaries.Items.Should().HaveCount(1);
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Update_ShouldModify_Post_WhenItExists()
    {
        // Arrange
        var (repository, databasePath) = CreateRepository();
        var post = await repository.CreateAsync(new CreateBlogPostDto { Title = "Old title", Content = "Old content" }, 1);

        try
        {
            var controller = CreateController(repository);
            var input = new CreateBlogPostDto
            {
                Title = "Updated title",
                Content = "Updated content"
            };

            // Act
            var result = await controller.Update(post.Id, input);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();

            var dto = okResult!.Value as BlogPostDto;
            dto.Should().NotBeNull();
            dto!.Title.Should().Be(input.Title);
            dto.Content.Should().Be(input.Content);
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Delete_ShouldRemove_Post_AndComments()
    {
        // Arrange
        var (repository, databasePath) = CreateRepository();
        var post = await repository.CreateAsync(new CreateBlogPostDto { Title = "Post 1", Content = "Content 1" }, 1);
        await repository.AddCommentAsync(post.Id, new CreateCommentDto { Text = "Comment 1" }, 1);

        try
        {
            var controller = CreateController(repository);

            // Act
            var result = await controller.Delete(post.Id);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            (await repository.GetSummariesAsync(new PaginationQueryDto())).Items.Should().BeEmpty();
            (await repository.GetCommentsAsync(post.Id, new PaginationQueryDto())).Should().BeNull();
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task GetComments_ShouldReturn_Comments_ForPost()
    {
        // Arrange
        var (repository, databasePath) = CreateRepository();
        var post = await repository.CreateAsync(new CreateBlogPostDto { Title = "Post 1", Content = "Content 1" }, 1);
        await repository.AddCommentAsync(post.Id, new CreateCommentDto { Text = "Comment 1" }, 1);
        await repository.AddCommentAsync(post.Id, new CreateCommentDto { Text = "Comment 2" }, 1);

        try
        {
            var controller = CreateController(repository);

            // Act
            var result = await controller.GetComments(post.Id, new PaginationQueryDto());

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();

            var comments = okResult!.Value as PagedResultDto<CommentDto>;
            comments.Should().NotBeNull();
            comments!.Items.Should().HaveCount(2);
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task UpdateComment_ShouldModify_Comment_WhenItExists()
    {
        // Arrange
        var (repository, databasePath) = CreateRepository();
        var post = await repository.CreateAsync(new CreateBlogPostDto { Title = "Post 1", Content = "Content 1" }, 1);
        var comment = await repository.AddCommentAsync(post.Id, new CreateCommentDto { Text = "Old comment" }, 1);

        try
        {
            var controller = CreateController(repository);
            var input = new CreateCommentDto { Text = "Updated comment" };

            // Act
            var result = await controller.UpdateComment(post.Id, comment!.Id, input);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();

            var dto = okResult!.Value as CommentDto;
            dto.Should().NotBeNull();
            dto!.Text.Should().Be(input.Text);
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task DeleteComment_ShouldRemove_Comment_WhenItExists()
    {
        // Arrange
        var (repository, databasePath) = CreateRepository();
        var post = await repository.CreateAsync(new CreateBlogPostDto { Title = "Post 1", Content = "Content 1" }, 1);
        var comment = await repository.AddCommentAsync(post.Id, new CreateCommentDto { Text = "Comment 1" }, 1);

        try
        {
            var controller = CreateController(repository);

            // Act
            var result = await controller.DeleteComment(post.Id, comment!.Id);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            (await repository.GetCommentsAsync(post.Id, new PaginationQueryDto()))!.Items.Should().BeEmpty();
        }
        finally
        {
            File.Delete(databasePath);
        }
    }
}

