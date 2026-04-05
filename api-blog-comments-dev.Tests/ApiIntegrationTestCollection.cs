using Xunit;

namespace api_blog_comments_dev.Tests;

[CollectionDefinition("ApiIntegrationTests", DisableParallelization = true)]
public sealed class ApiIntegrationTestCollection : ICollectionFixture<ApiApplicationFactory>
{
}