using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using CulinaryBlog.Application.Abstractions.FileStorage;
using CulinaryBlog.Application.Abstractions.Jobs;
using CulinaryBlog.Application.Abstractions.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.BackgroundJobs;

public sealed class SitemapWriter(
    IRecipeRepository recipeRepository,
    ICategoryRepository categoryRepository,
    IFileStorageService fileStorage,
    IConfiguration configuration,
    ILogger<SitemapWriter> logger) : ISitemapWriter
{
    private const string SitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private const int MaximumUrlCount = 50_000;
    private static readonly Action<ILogger, int, Exception?> LogSitemapUploaded =
        LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(9301, "SitemapUploaded"),
            "Uploaded sitemap.xml with {UrlCount} URLs.");

    public async Task GenerateAsync(CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["PublicSite:BaseUrl"];
        if (!Uri.TryCreate(
                string.IsNullOrWhiteSpace(baseUrl) ? null : $"{baseUrl.TrimEnd('/')}/",
                UriKind.Absolute,
                out var siteBaseUri)
            || siteBaseUri.Scheme is not ("http" or "https")
            || !string.IsNullOrEmpty(siteBaseUri.Query)
            || !string.IsNullOrEmpty(siteBaseUri.Fragment))
        {
            throw new InvalidOperationException(
                "PublicSite:BaseUrl must be an absolute HTTP or HTTPS URL without a query or fragment.");
        }

        var recipes = await recipeRepository.ListPublishedAsync(cancellationToken);
        var categories = await categoryRepository.ListAsync(cancellationToken);
        var urlCount = recipes.Count + categories.Count + 3;
        if (urlCount > MaximumUrlCount)
        {
            throw new InvalidOperationException(
                $"Sitemap contains {urlCount} URLs, exceeding the limit of {MaximumUrlCount}.");
        }

        XNamespace sitemap = SitemapNamespace;
        var urlElements = new List<XElement>(urlCount)
        {
            CreateUrlElement(sitemap, siteBaseUri, string.Empty, null, "daily", "1.0"),
            CreateUrlElement(sitemap, siteBaseUri, "recipes", null, "daily", "0.9"),
            CreateUrlElement(sitemap, siteBaseUri, "search", null, "weekly", "0.7"),
        };

        urlElements.AddRange(recipes.Select(recipe =>
            CreateUrlElement(
                sitemap,
                siteBaseUri,
                $"recipes/{recipe.Id:D}",
                recipe.UpdatedAt ?? recipe.PublishedAt ?? recipe.CreatedAt,
                "weekly",
                "0.8")));

        urlElements.AddRange(categories.Select(category =>
        {
            var categoryUri = new UriBuilder(new Uri(siteBaseUri, "recipes"))
            {
                Query = $"categoryId={Uri.EscapeDataString(category.Id.ToString("D"))}",
            };
            return CreateUrlElement(sitemap, categoryUri.Uri, null, "weekly", "0.6");
        }));

        var document = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(sitemap + "urlset", urlElements));

        await using var content = new MemoryStream();
        var writerSettings = new XmlWriterSettings
        {
            Async = false,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
        };
        using (var xmlWriter = XmlWriter.Create(content, writerSettings))
        {
            document.Save(xmlWriter);
        }

        content.Position = 0;
        await fileStorage.UploadAsync(
            content,
            "sitemap.xml",
            "application/xml; charset=utf-8",
            cancellationToken);
        LogSitemapUploaded(logger, urlCount, null);
    }

    private static XElement CreateUrlElement(
        XNamespace sitemap,
        Uri siteBaseUri,
        string relativePath,
        DateTimeOffset? lastModified,
        string changeFrequency,
        string priority) =>
        CreateUrlElement(
            sitemap,
            new Uri(siteBaseUri, relativePath),
            lastModified,
            changeFrequency,
            priority);

    private static XElement CreateUrlElement(
        XNamespace sitemap,
        Uri location,
        DateTimeOffset? lastModified,
        string changeFrequency,
        string priority)
    {
        var element = new XElement(
            sitemap + "url",
            new XElement(sitemap + "loc", location.AbsoluteUri));
        if (lastModified.HasValue)
        {
            element.Add(new XElement(
                sitemap + "lastmod",
                lastModified.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        }

        element.Add(
            new XElement(sitemap + "changefreq", changeFrequency),
            new XElement(sitemap + "priority", priority));
        return element;
    }
}
