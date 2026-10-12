using CulinaryBlog.Application.Categories;
using CulinaryBlog.Application.Common.Behaviors;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CulinaryBlog.API.Observability;

public static class ObservabilityExtensions
{
    private const string ServiceName = "CulinaryBlog.API";

    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var openTelemetry = services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName));

        openTelemetry.WithTracing(builder => builder
            .AddAspNetCoreInstrumentation(options =>
            {
                options.Filter = context => !context.Request.Path.StartsWithSegments("/health/live");
                options.RecordException = true;
            })
            .AddHttpClientInstrumentation()
            // Parameter values are not exported by default; do not enable them, they can carry emails or credentials.
            .AddEntityFrameworkCoreInstrumentation());

        openTelemetry.WithMetrics(builder => builder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter(CategoryMetrics.MeterName, RequestMetrics.MeterName));

        // Seq ingests traces over OTLP/HTTP only, so traces and metrics may use different endpoints.
        var protocol = string.Equals(configuration["OpenTelemetry:Protocol"], "HttpProtobuf", StringComparison.OrdinalIgnoreCase)
            ? OtlpExportProtocol.HttpProtobuf
            : OtlpExportProtocol.Grpc;

        if (ResolveEndpoint(configuration, "TracesEndpoint") is { } tracesEndpoint)
        {
            openTelemetry.WithTracing(builder => builder.AddOtlpExporter(options =>
            {
                options.Endpoint = tracesEndpoint;
                options.Protocol = protocol;
            }));
        }

        if (ResolveEndpoint(configuration, "MetricsEndpoint") is { } metricsEndpoint)
        {
            openTelemetry.WithMetrics(builder => builder.AddOtlpExporter(options =>
            {
                options.Endpoint = metricsEndpoint;
                options.Protocol = protocol;
            }));
        }

        return services;
    }

    private static Uri? ResolveEndpoint(IConfiguration configuration, string signalKey)
    {
        var endpoint = configuration[$"OpenTelemetry:{signalKey}"];
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            endpoint = configuration["OpenTelemetry:OtlpEndpoint"];
        }

        return Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ? uri : null;
    }
}
