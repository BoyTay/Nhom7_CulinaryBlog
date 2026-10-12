using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using CulinaryBlog.Application.Abstractions.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace CulinaryBlog.Integration.Tests;

public sealed class SensitiveLoggingTests : IClassFixture<SensitiveLoggingTests.LoggingFactory>
{
    private const string Token = "eyJhbGciOiJIUzI1NiJ9.sensitive-access-token.signature";
    private const string CookieValue = "sensitive-cookie-value";
    private const string Password = "Sup3r-Secret-Password";

    private readonly LoggingFactory _factory;

    public SensitiveLoggingTests(LoggingFactory factory) => _factory = factory;

    [Fact]
    public async Task RequestAndErrorLogsNeverContainTokensCookiesOrPasswords()
    {
        var client = _factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/v1/categories", UriKind.Relative))
        {
            Content = new StringContent($"{{\"name\":\"{Password}\",", Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        request.Headers.Add("Cookie", $"refresh={CookieValue}");
        using var response = await client.SendAsync(request);
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);

        // Without a database this ends in a handled 500, which exercises the exception-logging path too.
        using var list = await client.GetAsync(new Uri("/api/v1/categories", UriKind.Relative));
        Assert.NotNull(list);

        var entries = _factory.Entries;
        Assert.NotEmpty(entries);
        foreach (var entry in entries)
        {
            Assert.DoesNotContain(Token, entry, StringComparison.Ordinal);
            Assert.DoesNotContain(CookieValue, entry, StringComparison.Ordinal);
            Assert.DoesNotContain(Password, entry, StringComparison.Ordinal);
            Assert.DoesNotContain("culinary-local-only", entry, StringComparison.Ordinal);
        }
    }

    public sealed class LoggingFactory : WebApplicationFactory<Program>
    {
        private readonly CapturingSink _sink = new();

        public IReadOnlyList<string> Entries => _sink.Snapshot();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
                services.AddSingleton<IDataSession>(new NoopSession()));
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseSerilog((_, configuration) => configuration
                .Enrich.FromLogContext()
                .WriteTo.Sink(_sink));
            return base.CreateHost(builder);
        }
    }

    private sealed class NoopSession : IDataSession
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class CapturingSink : ILogEventSink
    {
        private readonly List<string> _entries = [];

        public void Emit(LogEvent logEvent)
        {
            var text = string.Join(' ',
                logEvent.RenderMessage(CultureInfo.InvariantCulture),
                string.Join(' ', logEvent.Properties.Select(property => $"{property.Key}={property.Value}")),
                logEvent.Exception);
            lock (_entries)
            {
                _entries.Add(text);
            }
        }

        public IReadOnlyList<string> Snapshot()
        {
            lock (_entries)
            {
                return [.. _entries];
            }
        }
    }
}
