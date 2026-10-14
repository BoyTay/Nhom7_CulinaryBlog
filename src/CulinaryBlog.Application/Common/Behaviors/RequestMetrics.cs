using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CulinaryBlog.Application.Common.Behaviors;

public static class RequestMetrics
{
    public const string MeterName = "CulinaryBlog.Application";

    private static readonly Meter Meter = new(MeterName, "1.0.0");

    private static readonly Histogram<double> Duration =
        Meter.CreateHistogram<double>("culinaryblog.request.duration", "ms", "MediatR request duration.");

    private static readonly Counter<long> Errors =
        Meter.CreateCounter<long>("culinaryblog.request.errors", "{request}", "MediatR requests that threw.");

    public static void RecordCompleted(string requestName, double elapsedMilliseconds) =>
        Duration.Record(elapsedMilliseconds, new TagList { { "request", requestName }, { "outcome", "success" } });

    public static void RecordFailed(string requestName, string exceptionType, double elapsedMilliseconds)
    {
        Duration.Record(elapsedMilliseconds, new TagList { { "request", requestName }, { "outcome", "error" } });
        Errors.Add(1, new TagList { { "request", requestName }, { "exception", exceptionType } });
    }
}
