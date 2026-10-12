using System.Diagnostics.Metrics;

namespace CulinaryBlog.Application.Categories;

public static class CategoryMetrics
{
    public const string MeterName = "CulinaryBlog.Categories";

    private static readonly Meter Meter = new(MeterName, "1.0.0");

    private static readonly Counter<long> Created =
        Meter.CreateCounter<long>("culinaryblog.category.created", "{category}", "Categories created.");

    private static readonly Counter<long> Updated =
        Meter.CreateCounter<long>("culinaryblog.category.updated", "{category}", "Categories updated.");

    private static readonly Counter<long> Deleted =
        Meter.CreateCounter<long>("culinaryblog.category.deleted", "{category}", "Categories soft-deleted.");

    public static void RecordCreated() => Created.Add(1);

    public static void RecordUpdated() => Updated.Add(1);

    public static void RecordDeleted() => Deleted.Add(1);
}
