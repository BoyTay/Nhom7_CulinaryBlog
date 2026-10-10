using CulinaryBlog.Application.Common.Behaviors;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Application.Tests;

public sealed class RequestTimingBehaviorTests
{
    [Fact]
    public async Task LogsSlowRequestWithoutItsPayload()
    {
        var clock = new ManualTimeProvider();
        var logger = new RecordingLogger<RequestTimingBehavior<ProbeRequest, string>>();
        var behavior = new RequestTimingBehavior<ProbeRequest, string>(clock, logger);

        var result = await behavior.Handle(new ProbeRequest("secret-token"), _ =>
        {
            clock.Advance(501);
            return Task.FromResult("done");
        }, default);

        Assert.Equal("done", result);
        Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, logger.Entries[0].Level);
        Assert.Equal(7002, logger.Entries[0].EventId.Id);
        Assert.DoesNotContain("secret-token", logger.Entries[0].Message);
    }

    [Fact]
    public async Task LogsNormalRequestAndRethrowsFailure()
    {
        var clock = new ManualTimeProvider();
        var logger = new RecordingLogger<RequestTimingBehavior<ProbeRequest, string>>();
        var behavior = new RequestTimingBehavior<ProbeRequest, string>(clock, logger);

        await behavior.Handle(new ProbeRequest("secret-token"), _ =>
        {
            clock.Advance(500);
            return Task.FromResult("done");
        }, default);
        Assert.Equal(LogLevel.Information, logger.Entries[0].Level);
        Assert.Equal(7001, logger.Entries[0].EventId.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(
            new ProbeRequest("secret-token"), _ => throw new InvalidOperationException("failure"), default));
        Assert.Equal(7003, logger.Entries[1].EventId.Id);
        Assert.DoesNotContain("secret-token", logger.Entries[1].Message);
    }

    private sealed record ProbeRequest(string Secret);

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long _milliseconds;
        public override long TimestampFrequency => 1000;
        public override long GetTimestamp() => _milliseconds;
        public void Advance(long milliseconds) => _milliseconds += milliseconds;
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, EventId EventId, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, eventId, formatter(state, exception)));
    }
}
