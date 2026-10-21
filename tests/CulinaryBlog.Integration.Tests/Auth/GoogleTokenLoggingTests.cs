using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using CulinaryBlog.Application.Common.Exceptions;
using CulinaryBlog.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Integration.Tests.Auth;

public sealed class GoogleTokenLoggingTests
{
    [Fact]
    public async Task GoogleValidationDoesNotLogExceptionPayloadOrEchoUntrustedIssuer()
    {
        const string privateValue = "private-credential-and-personal-data";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Google:ClientId"] = "test-client",
        }).Build();
        var logger = new CaptureLogger();
        using var client = new HttpClient(new FailingHandler(privateValue));
        var validator = new GoogleTokenValidator(new ClientFactory(client), configuration, logger);
        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: "https://accounts.google.com", audience: "test-client",
            claims: [new Claim("sub", "subject"), new Claim("email", "private@example.test")],
            expires: DateTime.UtcNow.AddMinutes(10)));

        var unavailable = await Assert.ThrowsAsync<UnauthorizedException>(() => validator.ValidateAsync(token));
        Assert.Equal("GOOGLE_SERVICE_UNAVAILABLE", unavailable.Code);
        Assert.NotEmpty(logger.Messages);
        Assert.DoesNotContain(privateValue, string.Join('\n', logger.Messages), StringComparison.Ordinal);
        Assert.DoesNotContain(token, string.Join('\n', logger.Messages), StringComparison.Ordinal);

        var untrusted = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: privateValue, audience: "test-client", expires: DateTime.UtcNow.AddMinutes(10)));
        var invalid = await Assert.ThrowsAsync<UnauthorizedException>(() => validator.ValidateAsync(untrusted));
        Assert.Equal("INVALID_GOOGLE_TOKEN", invalid.Code);
        Assert.DoesNotContain(privateValue, invalid.Message, StringComparison.Ordinal);
    }

    private sealed class FailingHandler(string privateValue) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(new HttpRequestException(privateValue, null, HttpStatusCode.BadGateway));
    }

    private sealed class ClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class CaptureLogger : ILogger<GoogleTokenValidator>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception) + exception);
    }
}
