using System.Text.RegularExpressions;
using CulinaryBlog.Application.Abstractions.Search;
using CulinaryBlog.Application.Common.Pagination;
using CulinaryBlog.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CulinaryBlog.Infrastructure.Persistence;

public sealed class RecipeSearchReader(ApplicationDbContext dbContext) : IRecipeSearchReader
{
    private static readonly Regex SearchTokenPattern = new(@"[\p{L}\p{M}\p{N}]+", RegexOptions.Compiled);

    public async Task<PagedResult<RecipeSearchResult>> SearchAsync(
        RecipeSearchOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        options = options.Validate();

        var searchQuery = BuildPrefixSearchQuery(options.SearchTerm);
        if (options.SearchTerm is not null && searchQuery.Length == 0)
        {
            return PagedResult.Create<RecipeSearchResult>([], 0, options.Page, options.PageSize);
        }

        var parameters = new List<NpgsqlParameter>();
        var filters = new List<string>
        {
            "r.\"Status\" = 'Published'",
            "r.\"IsDeleted\" = FALSE",
        };

        if (searchQuery.Length > 0)
        {
            filters.Add(
                "r.\"SearchVector\" @@ to_tsquery('simple', public.unaccent('public.unaccent', @searchQuery))");
            parameters.Add(new NpgsqlParameter("searchQuery", searchQuery));
        }

        if (options.CategoryId is { } categoryId)
        {
            filters.Add("r.\"CategoryId\" = @categoryId");
            parameters.Add(new NpgsqlParameter("categoryId", categoryId));
        }

        if (!string.IsNullOrWhiteSpace(options.Difficulty))
        {
            if (!Enum.TryParse<RecipeDifficulty>(options.Difficulty, true, out var difficulty) ||
                !Enum.IsDefined(difficulty))
            {
                throw new ArgumentException(
                    $"Unsupported recipe difficulty '{options.Difficulty}'.",
                    nameof(options));
            }

            filters.Add("r.\"Difficulty\" = @difficulty");
            parameters.Add(new NpgsqlParameter("difficulty", difficulty.ToString()));
        }

        if (options.MaxCookTime is { } maxCookTime)
        {
            filters.Add("r.\"CookTimeMinutes\" <= @maxCookTime");
            parameters.Add(new NpgsqlParameter("maxCookTime", maxCookTime));
        }

        if (options.MinServings is { } minServings)
        {
            filters.Add("r.\"Servings\" >= @minServings");
            parameters.Add(new NpgsqlParameter("minServings", minServings));
        }

        var whereClause = string.Join(" AND ", filters);
        var countSql = string.Concat(
            "SELECT COUNT(*)::integer AS \"Value\" FROM \"Recipes\" AS r WHERE ",
            whereClause);
        var totalCount = await dbContext.Database
            .SqlQueryRaw<int>(
                countSql,
                parameters.ToArray())
            .SingleAsync(cancellationToken);

        parameters.Add(new NpgsqlParameter("pageSize", options.PageSize));
        parameters.Add(new NpgsqlParameter(
            "offset",
            (int)Math.Min(((long)options.Page - 1) * options.PageSize, int.MaxValue)));

        var rankExpression = searchQuery.Length > 0
            ? "ts_rank(r.\"SearchVector\", to_tsquery('simple', public.unaccent('public.unaccent', @searchQuery)))::double precision"
            : "NULL::double precision";
        var orderClause = BuildOrderClause(options.Sort, rankExpression);
        var searchSql = string.Concat(
            "SELECT\n",
            "    r.\"Id\" AS \"Id\",\n",
            "    r.\"Slug\" AS \"Slug\",\n",
            "    r.\"Title\" AS \"Title\",\n",
            "    r.\"Description\" AS \"Description\",\n",
            "    r.\"CategoryId\" AS \"CategoryId\",\n",
            "    r.\"Difficulty\" AS \"Difficulty\",\n",
            "    r.\"CookTimeMinutes\" AS \"CookTimeMinutes\",\n",
            "    COALESCE(r.\"PublishedAt\", r.\"CreatedAt\") AS \"PublishedAt\",\n",
            rankExpression,
            " AS \"RelevanceScore\"\n",
            "FROM \"Recipes\" AS r\n",
            "WHERE ",
            whereClause,
            "\nORDER BY ",
            orderClause,
            "\nLIMIT @pageSize OFFSET @offset");
        var rows = await dbContext.Database
            .SqlQueryRaw<SearchRow>(
                searchSql,
                parameters.ToArray())
            .ToListAsync(cancellationToken);

        var results = rows
            .Select(row => new RecipeSearchResult(
                row.Id,
                row.Slug,
                row.Title,
                row.Description,
                row.CategoryId,
                row.Difficulty,
                row.CookTimeMinutes,
                row.PublishedAt,
                row.RelevanceScore))
            .ToList();

        return PagedResult.Create(results, totalCount, options.Page, options.PageSize);
    }

    private static string BuildPrefixSearchQuery(string? searchTerm)
    {
        if (searchTerm is null)
        {
            return string.Empty;
        }

        var tokens = SearchTokenPattern
            .Matches(searchTerm)
            .Select(match => $"{match.Value}:*");

        return string.Join(" & ", tokens);
    }

    private static string BuildOrderClause(string sort, string rankExpression)
    {
        var descending = sort.StartsWith('-');
        var sortField = descending ? sort[1..] : sort;
        var direction = descending ? "DESC" : "ASC";

        return sortField switch
        {
            "createdAt" => $"r.\"CreatedAt\" {direction}, r.\"Id\" ASC",
            "title" => $"r.\"Title\" {direction}, r.\"Id\" ASC",
            "cookTime" => $"r.\"CookTimeMinutes\" {direction}, r.\"Id\" ASC",
            "relevance" => $"{rankExpression} {direction}, r.\"CreatedAt\" DESC, r.\"Id\" ASC",
            _ => throw new ArgumentException($"Unsupported sort field '{sort}'.", nameof(sort)),
        };
    }

    private sealed class SearchRow
    {
        public Guid Id { get; set; }

        public string Slug { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public Guid CategoryId { get; set; }

        public string Difficulty { get; set; } = string.Empty;

        public int CookTimeMinutes { get; set; }

        public DateTimeOffset PublishedAt { get; set; }

        public double? RelevanceScore { get; set; }
    }
}
