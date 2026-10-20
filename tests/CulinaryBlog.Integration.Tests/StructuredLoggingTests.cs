using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Core;
using Serilog.Events;

namespace CulinaryBlog.Integration.Tests;

public sealed class StructuredLoggingTests : IClassFixture<StructuredLoggingTests.LoggingFactory>
{
    private static readonly Uri RequestUri = new("/openapi/v1.json", UriKind.Relative);
    private readonly LoggingFactory _factory;

    public StructuredLoggingTests(LoggingFactory factory) => _factory = factory;

    [Fact]
    public async Task RequestLogCarriesMethodPathStatusElapsedAndEchoedCorrelationId()
    {
        var correlationId = Guid.NewGuid().ToString("N");
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, RequestUri);
        request.Headers.Add("X-Correlation-ID", correlationId);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-ID").Single());
        var entry = RequestCompleted(correlationId);
        Assert.Equal("GET", Scalar(entry, "RequestMethod"));
        Assert.Equal("/openapi/v1.json", Scalar(entry, "RequestPath"));
        Assert.Equal("200", Scalar(entry, "StatusCode"));
        Assert.True(entry.Properties.ContainsKey("Elapsed"));
    }

    [Fact]
    public async Task CorrelationIdIsGeneratedWhenMissingAndUnauthenticatedLogHasNoUserId()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(RequestUri);

        var generated = response.Headers.GetValues("X-Correlation-ID").Single();
        Assert.Matches("^[0-9a-f]{32}$", generated);
        Assert.False(RequestCompleted(generated).Properties.ContainsKey("UserId"));
    }

    [Fact]
    public async Task AuthenticatedRequestLogCarriesOnlyTheOpaqueUserId()
    {
        var correlationId = Guid.NewGuid().ToString("N");
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, RequestUri);
        request.Headers.Add("X-Correlation-ID", correlationId);
        request.Headers.Add("X-Test-User", "user-42");

        using var response = await client.SendAsync(request);

        var entry = RequestCompleted(correlationId);
        Assert.Equal("user-42", Scalar(entry, "UserId"));
        Assert.DoesNotContain(entry.Properties, property => property.Value.ToString().Contains("user-42@example.test", StringComparison.Ordinal));
    }

    // The client can receive the response before Serilog's request middleware has emitted its event, so poll briefly.
    private LogEvent RequestCompleted(string correlationId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (true)
        {
            var match = _factory.Entries.SingleOrDefault(entry =>
                entry.MessageTemplate.Text.StartsWith("HTTP {RequestMethod}", StringComparison.Ordinal) &&
                Scalar(entry, "CorrelationId") == correlationId);
            if (match is not null)
            {
                return match;
            }

            if (DateTime.UtcNow > deadline)
            {
                throw new Xunit.Sdk.XunitException($"No request-completed log event for correlation id {correlationId}.");
            }

            Thread.Sleep(25);
        }
    }

    private static string? Scalar(LogEvent entry, string name) =>
        entry.Properties.TryGetValue(name, out var value) && value is ScalarValue scalar
            ? Convert.ToString(scalar.Value, System.Globalization.CultureInfo.InvariantCulture)
            : null;

    public sealed class LoggingFactory : WebApplicationFactory<Program>
    {
        private readonly CapturingSink _sink = new();

        public IReadOnlyList<LogEvent> Entries => _sink.Snapshot();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<ILogEventSink>(_sink);
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                }).AddScheme<AuthenticationSchemeOptions, UserHeaderAuthHandler>("Test", _ => { });
            });
        }
    }

    private sealed class UserHeaderAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-User", out var user))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, user.ToString()), new Claim(ClaimTypes.Email, $"{user}@example.test")],
                Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
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
