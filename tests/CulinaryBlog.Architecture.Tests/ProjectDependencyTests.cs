using System.Xml.Linq;

namespace CulinaryBlog.Architecture.Tests;

public sealed class ProjectDependencyTests
{
    [Fact]
    public void DomainMustNotReferenceOtherSolutionProjects()
    {
        var references = ReadProjectReferences("src", "CulinaryBlog.Domain", "CulinaryBlog.Domain.csproj");

        Assert.Empty(references);
    }

    [Fact]
    public void ApplicationMustOnlyReferenceDomain()
    {
        var references = ReadProjectReferences(
            "src",
            "CulinaryBlog.Application",
            "CulinaryBlog.Application.csproj");

        Assert.Equal(["CulinaryBlog.Domain"], references);
    }

    [Fact]
    public void InfrastructureMustNotReferenceApi()
    {
        var references = ReadProjectReferences(
            "src",
            "CulinaryBlog.Infrastructure",
            "CulinaryBlog.Infrastructure.csproj");

        Assert.DoesNotContain("CulinaryBlog.API", references);
    }

    private static string[] ReadProjectReferences(params string[] pathParts)
    {
        var repositoryRoot = FindRepositoryRoot();
        var projectPath = Path.Combine([repositoryRoot, .. pathParts]);
        var document = XDocument.Load(projectPath);

        return document
            .Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFileNameWithoutExtension(path!))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CulinaryBlog.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
