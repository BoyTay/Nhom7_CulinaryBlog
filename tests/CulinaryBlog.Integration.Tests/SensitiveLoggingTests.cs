
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using CulinaryBlog.Application.Categories;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;

namespace CulinaryBlog.Integration.Tests;

public sealed class SensitiveLoggingTests
    : IClassFixture<SensitiveLoggingTests.LoggingFactory>
{
    private const string Token =
        "eyJhbGciOiJIUzI1NiJ9.sensitive-access-token.signature";

    private const string CookieValue = "sensitive-cookie-value";
    private const string Password = "Sup3r-Secret-Password";

    private readonly LoggingFactory _factory;

    public SensitiveLoggingTests(LoggingFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RequestAndErrorLogsNeverContainTokensCookiesOrPasswords()
    {
        using var client = _factory.CreateClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri("/api/v1/categories", UriKind.Relative))
        {
            Content = new StringContent(
                $"{{\"name\":\"{Password}\",",
                Encoding.UTF8,
                "application/json"),
        };

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);

        request.Headers.Add("Cookie", $"refresh={CookieValue}");

        using var response = await client.SendAsync(request);

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);

        // Force a controlled application failure.
        using var list = await client.GetAsync(
            new Uri("/api/v1/categories", UriKind.Relative));

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            list.StatusCode);

        var entries = _factory.Entries;

        Assert.NotEmpty(entries);

        // Verify the structured HTTP request completion event.
        Assert.Contains(entries, entry =>
            entry.Properties.TryGetValue("SourceContext", out var source) &&
            source.ToString().Contains(
                "Microsoft.AspNetCore.Hosting.Diagnostics",
                StringComparison.Ordinal) &&
            entry.MessageTemplate.Text.StartsWith(
                "Request finished",
                StringComparison.Ordinal) &&
            entry.Properties.ContainsKey("Method") &&
            entry.Properties.ContainsKey("RequestPath") &&
            entry.Properties.ContainsKey("StatusCode") &&
            entry.Properties.ContainsKey("ElapsedMilliseconds"));

        // Verify the global exception handler emitted a log.
        Assert.Contains(entries, entry =>
            entry.Properties.TryGetValue(
                "SourceContext", out var source) &&
            source.ToString().Contains(
                "CulinaryBlog.API.ErrorHandling.GlobalExceptionHandler",
                StringComparison.Ordinal));

        // Check rendered messages, properties, templates and exceptions.
        foreach (var entry in entries)
        {
            var content = string.Join(' ',
                entry.MessageTemplate.Text,
                entry.RenderMessage(CultureInfo.InvariantCulture),
                string.Join(' ',
                    entry.Properties.Select(property =>
                        $"{property.Key}={property.Value}")),
                entry.Exception?.ToString() ?? string.Empty);

            Assert.DoesNotContain(
                Token, content, StringComparison.Ordinal);

            Assert.DoesNotContain(
                CookieValue, content, StringComparison.Ordinal);

            Assert.DoesNotContain(
                Password, content, StringComparison.Ordinal);

            Assert.DoesNotContain(
                "culinary-local-only", content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task LoggingRemainsAttachedToItsHostAfterAnotherHostIsDisposed()
    {
        using var client = _factory.CreateClient();

        await using (var otherFactory = new LoggingFactory())
        {
            using var otherClient = otherFactory.CreateClient();

            using var otherResponse = await otherClient.GetAsync(
                new Uri("/api/v1/categories", UriKind.Relative));

            Assert.Equal(
                HttpStatusCode.InternalServerError,
                otherResponse.StatusCode);
        }

        var correlationId = Guid.NewGuid().ToString("N");

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri("/api/v1/categories", UriKind.Relative));

        request.Headers.Add("X-Correlation-ID", correlationId);

        using var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        Assert.Contains(_factory.Entries, entry =>
            entry.Properties.TryGetValue(
                "SourceContext", out var source) &&
            source.ToString().Contains(
                "CulinaryBlog.API.ErrorHandling.GlobalExceptionHandler",
                StringComparison.Ordinal) &&
            entry.Properties.Values.Any(value =>
                value.ToString().Contains(
                    correlationId, StringComparison.Ordinal)));
    }

    public sealed class LoggingFactory : WebApplicationFactory<Program>
    {
        private readonly CapturingSink _sink = new();

        public IReadOnlyList<LogEvent> Entries => _sink.Snapshot();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureTestServices(services =>
            {
                // Register the capturing sink through DI.
                services.AddSingleton<ILogEventSink>(_sink);

                // Produce a deterministic application error.
                services.AddTransient<
                    IRequestHandler<
                        GetCategoriesQuery,
                        IReadOnlyList<CategoryDto>>,
                    FailingCategoryQueryHandler>();
            });
        }
    }

    private sealed class FailingCategoryQueryHandler
        : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
    {
        public Task<IReadOnlyList<CategoryDto>> Handle(
            GetCategoriesQuery request,
            CancellationToken cancellationToken)
        {
            return Task.FromException<IReadOnlyList<CategoryDto>>(
                new InvalidOperationException(
                    "Controlled logging test failure."));
        }
    }

    private sealed class CapturingSink : ILogEventSink
    {
        private readonly List<LogEvent> _entries = [];

        public void Emit(LogEvent logEvent)
        {
            lock (_entries)
            {
                _entries.Add(logEvent);
            }
        }

        public IReadOnlyList<LogEvent> Snapshot()
        {
            lock (_entries)
            {
                return [.. _entries];
            }
        }
    }
}
