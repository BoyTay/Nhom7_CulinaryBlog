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
                options.Filter = context => !context.Request.Path.StartsWithSegments("/health/live"))
            .AddHttpClientInstrumentation());

        openTelemetry.WithMetrics(builder => builder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation());

        var endpoint = configuration["OpenTelemetry:OtlpEndpoint"];
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
        {
            openTelemetry.WithTracing(builder =>
                builder.AddOtlpExporter(options => options.Endpoint = endpointUri));
            openTelemetry.WithMetrics(builder =>
                builder.AddOtlpExporter(options => options.Endpoint = endpointUri));
        }

        return services;
    }
}
