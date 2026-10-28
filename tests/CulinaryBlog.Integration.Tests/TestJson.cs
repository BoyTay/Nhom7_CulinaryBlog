using System.Text.Json;
using System.Text.Json.Serialization;

namespace CulinaryBlog.Integration.Tests;

/// <summary>Deserialization options that match the API: web defaults plus enum names.</summary>
internal static class TestJson
{
    public static readonly JsonSerializerOptions Options = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
