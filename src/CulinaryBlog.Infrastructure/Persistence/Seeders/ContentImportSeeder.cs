namespace CulinaryBlog.Infrastructure.Persistence.Seeders;

using System.Text.Json;
using System.Text.Json.Serialization;
using CulinaryBlog.Application.Auth.Common;
using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>
/// Imports categories and published recipes from a JSON file (see <c>seed/content.sample.json</c>).
/// Opt-in through <c>Seed:ContentFile</c>. Safe to run repeatedly: rows are matched by slug and never overwritten.
/// </summary>
public sealed class ContentImportSeeder(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    ILogger<ContentImportSeeder> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly Action<ILogger, string, Exception?> LogFileMissing =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(6101, "ContentFileMissing"), "Content file not found: {Path}");

    private static readonly Action<ILogger, Exception?> LogNoAuthor =
        LoggerMessage.Define(LogLevel.Warning, new EventId(6102, "ContentNoAuthor"), "No user found to own the imported recipes; create an account (or set Admin:Email) and restart.");

    private static readonly Action<ILogger, string, string, Exception?> LogRecipeSkipped =
        LoggerMessage.Define<string, string>(LogLevel.Warning, new EventId(6103, "ContentRecipeSkipped"), "Skipped recipe {Slug}: {Reason}");

    private static readonly Action<ILogger, int, int, Exception?> LogImported =
        LoggerMessage.Define<int, int>(LogLevel.Information, new EventId(6104, "ContentImported"), "Imported {Categories} categories and {Recipes} recipes.");

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var path = configuration["Seed:ContentFile"];
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (!File.Exists(path))
        {
            LogFileMissing(logger, path, null);
            return;
        }

        await using var stream = File.OpenRead(path);
        var content = await JsonSerializer.DeserializeAsync<ContentFile>(stream, JsonOptions, cancellationToken)
            ?? new ContentFile();

        var authorId = await ResolveAuthorIdAsync();
        if (authorId is null)
        {
            LogNoAuthor(logger, null);
            return;
        }

        var categoriesBySlug = await dbContext.Categories.ToDictionaryAsync(c => c.Slug, cancellationToken);
        var newCategories = 0;
        foreach (var item in content.Categories)
        {
            if (categoriesBySlug.ContainsKey(item.Slug))
            {
                continue;
            }

            var category = Category.Create(item.Name, item.Slug, item.Description, item.ImageUrl, item.OrderIndex);
            dbContext.Categories.Add(category);
            categoriesBySlug[item.Slug] = category;
            newCategories++;
        }

        var existingSlugs = (await dbContext.Recipes.Select(r => r.Slug).ToListAsync(cancellationToken)).ToHashSet();
        var imageBaseUrl = ResolveImageBaseUrl();
        var newRecipes = 0;
        foreach (var item in content.Recipes)
        {
            if (existingSlugs.Contains(item.Slug))
            {
                continue;
            }

            if (!categoriesBySlug.TryGetValue(item.CategorySlug, out var category))
            {
                LogRecipeSkipped(logger, item.Slug, $"unknown category '{item.CategorySlug}'", null);
                continue;
            }

            try
            {
                dbContext.Recipes.Add(BuildRecipe(item, category.Id, authorId, imageBaseUrl));
                existingSlugs.Add(item.Slug);
                newRecipes++;
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException
                or CulinaryBlog.Domain.Exceptions.DomainException)
            {
                LogRecipeSkipped(logger, item.Slug, ex.Message, null);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        LogImported(logger, newCategories, newRecipes, null);
    }

    private static Recipe BuildRecipe(RecipeItem item, Guid categoryId, string authorId, string imageBaseUrl)
    {
        var recipe = Recipe.Create(
            item.Title,
            item.Slug,
            item.Description,
            categoryId,
            authorId,
            item.PrepTimeMinutes,
            item.CookTimeMinutes,
            item.Servings,
            item.Difficulty);

        if (item.Nutrition is { } n)
        {
            recipe.Update(
                item.Title,
                item.Slug,
                item.Description,
                categoryId,
                item.PrepTimeMinutes,
                item.CookTimeMinutes,
                item.Servings,
                item.Difficulty,
                RecipeNutrition.Create(n.Calories, n.ProteinGrams, n.CarbohydratesGrams, n.FatGrams));
        }

        foreach (var ingredient in item.Ingredients)
        {
            recipe.AddIngredient(ingredient.Name, ingredient.Quantity, ingredient.Unit, ingredient.Notes);
        }

        foreach (var step in item.Steps)
        {
            recipe.AddStep(step.Description, step.TimerMinutes);
        }

        if (!string.IsNullOrWhiteSpace(item.ImageUrl))
        {
            var url = Uri.TryCreate(item.ImageUrl, UriKind.Absolute, out _)
                ? item.ImageUrl
                : imageBaseUrl + item.ImageUrl.TrimStart('/');
            recipe.AddImage(url, item.Title);
        }

        recipe.Publish();
        return recipe;
    }

    // Relative image names are served by the web app from public/images/recipes/.
    private string ResolveImageBaseUrl()
    {
        var configured = configuration["Seed:ImageBaseUrl"];
        var baseUrl = !string.IsNullOrWhiteSpace(configured)
            ? configured
            : (configuration["PublicSite:BaseUrl"] ?? "http://localhost").TrimEnd('/') + "/images/recipes/";
        return baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
    }

    private async Task<string?> ResolveAuthorIdAsync()
    {
        var adminEmail = configuration["Admin:Email"];
        if (!string.IsNullOrWhiteSpace(adminEmail) && await userManager.FindByEmailAsync(adminEmail) is { } admin)
        {
            return admin.Id;
        }

        var admins = await userManager.GetUsersInRoleAsync(Roles.Admin);
        if (admins.Count > 0)
        {
            return admins[0].Id;
        }

        return await userManager.Users.OrderBy(u => u.CreatedAt).Select(u => u.Id).FirstOrDefaultAsync();
    }

    private sealed class ContentFile
    {
        public List<CategoryItem> Categories { get; init; } = [];

        public List<RecipeItem> Recipes { get; init; } = [];
    }

    private sealed class CategoryItem
    {
        public string Name { get; init; } = string.Empty;

        public string Slug { get; init; } = string.Empty;

        public string? Description { get; init; }

        public string? ImageUrl { get; init; }

        public int OrderIndex { get; init; }
    }

    private sealed class RecipeItem
    {
        public string Title { get; init; } = string.Empty;

        public string Slug { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public string CategorySlug { get; init; } = string.Empty;

        public int PrepTimeMinutes { get; init; }

        public int CookTimeMinutes { get; init; }

        public int Servings { get; init; } = 1;

        public RecipeDifficulty Difficulty { get; init; }

        public NutritionItem? Nutrition { get; init; }

        public string? ImageUrl { get; init; }

        public List<IngredientItem> Ingredients { get; init; } = [];

        public List<StepItem> Steps { get; init; } = [];
    }

    private sealed class NutritionItem
    {
        public int Calories { get; init; }

        public decimal ProteinGrams { get; init; }

        public decimal CarbohydratesGrams { get; init; }

        public decimal FatGrams { get; init; }
    }

    private sealed class IngredientItem
    {
        public string Name { get; init; } = string.Empty;

        public decimal Quantity { get; init; }

        public string Unit { get; init; } = string.Empty;

        public string? Notes { get; init; }
    }

    private sealed class StepItem
    {
        public string Description { get; init; } = string.Empty;

        public int? TimerMinutes { get; init; }
    }
}
