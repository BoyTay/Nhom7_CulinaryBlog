using System.Net;
using System.Text.Json;
using CulinaryBlog.Application.Abstractions.Search;
using CulinaryBlog.Application.Common.Pagination;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CulinaryBlog.Integration.Tests;

public sealed class RecipeSearchEndpointTests : IClassFixture<RecipeSearchEndpointTests.SearchFactory>
{
    private readonly SearchFactory _factory;

    public RecipeSearchEndpointTests(SearchFactory factory) => _factory = factory;

    [Fact]
    public async Task SearchEndpointPassesFiltersAndPagingToReader()
    {
        var categoryId = Guid.NewGuid();
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/v1/recipes/search?q=%20soup%20&categoryId={categoryId}&difficulty=Easy&maxCookTime=45&minServings=3&sort=title&page=2&pageSize=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(_factory.Reader.LastOptions);
        Assert.Equal("soup", _factory.Reader.LastOptions.SearchTerm);
        Assert.Equal(categoryId, _factory.Reader.LastOptions.CategoryId);
        Assert.Equal("Easy", _factory.Reader.LastOptions.Difficulty);
        Assert.Equal(45, _factory.Reader.LastOptions.MaxCookTime);
        Assert.Equal(3, _factory.Reader.LastOptions.MinServings);
        Assert.Equal("title", _factory.Reader.LastOptions.Sort);
        Assert.Equal(2, _factory.Reader.LastOptions.Page);
        Assert.Equal(5, _factory.Reader.LastOptions.PageSize);

        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(2, result.RootElement.GetProperty("page").GetInt32());
        Assert.Equal(5, result.RootElement.GetProperty("pageSize").GetInt32());
        Assert.Equal(1, result.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task SearchEndpointDefaultsToRelevanceForSearchTermsAndNewestForBrowse()
    {
        var client = _factory.CreateClient();

        var searchResponse = await client.GetAsync("/api/v1/recipes/search?q=soup");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        Assert.Equal("-relevance", _factory.Reader.LastOptions?.Sort);

        var browseResponse = await client.GetAsync("/api/v1/recipes/search");

        Assert.Equal(HttpStatusCode.OK, browseResponse.StatusCode);
        Assert.Equal("-createdAt", _factory.Reader.LastOptions?.Sort);
        Assert.Equal(1, _factory.Reader.LastOptions?.Page);
    }

    [Fact]
    public async Task SearchEndpointReturns422ForShortSearchTerm()
    {
        var client = _factory.CreateClient();
        var searchCount = _factory.Reader.SearchCount;

        var response = await client.GetAsync("/api/v1/recipes/search?q=a");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(searchCount, _factory.Reader.SearchCount);
    }

    [Fact]
    public async Task SearchEndpointReturns422ForUnsupportedDifficulty()
    {
        var client = _factory.CreateClient();
        var searchCount = _factory.Reader.SearchCount;

        var response = await client.GetAsync("/api/v1/recipes/search?difficulty=Impossible");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(searchCount, _factory.Reader.SearchCount);
    }

    public sealed class SearchFactory : WebApplicationFactory<Program>
    {
        public CapturingSearchReader Reader { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IRecipeSearchReader>();
                services.AddSingleton<IRecipeSearchReader>(Reader);
            });
        }
    }

    public sealed class CapturingSearchReader : IRecipeSearchReader
    {
        public RecipeSearchOptions? LastOptions { get; private set; }

        public int SearchCount { get; private set; }

        public Task<PagedResult<RecipeSearchResult>> SearchAsync(
            RecipeSearchOptions options,
            CancellationToken cancellationToken = default)
        {
            SearchCount++;
            LastOptions = options;
            var results = PagedResult.Create(
                new[]
                {
                    new RecipeSearchResult(
                        Guid.NewGuid(),
                        "tomato-soup",
                        "Tomato soup",
                        "A simple soup recipe.",
                        options.CategoryId ?? Guid.NewGuid(),
                        "Easy",
                        20,
                        DateTimeOffset.UtcNow,
                        0.8),
                },
                1,
                options.Page,
                options.PageSize);

            return Task.FromResult(results);
        }
    }
}
