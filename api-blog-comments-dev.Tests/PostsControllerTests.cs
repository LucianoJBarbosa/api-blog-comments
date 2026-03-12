using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using api_blog_comments_dev.Controllers;
using api_blog_comments_dev.Data;
using api_blog_comments_dev.DTOs;
using api_blog_comments_dev.Models;

namespace api_blog_comments_dev.Tests;

public class PostsControllerTests
{
    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        return context;
    }

    [Fact]
    public async Task Get_ShouldReturn_ListOfSummaries()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        context.BlogPosts.Add(new BlogPost { Title = "Post 1", Content = "Content 1" });
        context.BlogPosts.Add(new BlogPost { Title = "Post 2", Content = "Content 2" });
        await context.SaveChangesAsync();

        var controller = new PostsController(context);

        // Act
        var result = await controller.Get();

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();

        var summaries = okResult!.Value as IEnumerable<BlogPostSummaryDto>;
        summaries.Should().NotBeNull();
        summaries!.Count().Should().Be(2);
    }

    [Fact]
    public async Task Create_ShouldPersist_Post_WhenModelIsValid()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var controller = new PostsController(context);

        var input = new CreateBlogPostDto
        {
            Title = "New Post",
            Content = "Post content"
        };

        // Act
        var result = await controller.Create(input);

        // Assert
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();

        var dto = createdResult!.Value as BlogPostDto;
        dto.Should().NotBeNull();
        dto!.Title.Should().Be(input.Title);

        context.BlogPosts.Count().Should().Be(1);
    }
}

