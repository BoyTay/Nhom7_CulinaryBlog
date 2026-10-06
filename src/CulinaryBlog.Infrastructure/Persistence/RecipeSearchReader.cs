using System.Data;
using CulinaryBlog.Application.Abstractions.Search;
using CulinaryBlog.Application.Common.Pagination;
using CulinaryBlog.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;

namespace CulinaryBlog.Infrastructure.Persistence;

public sealed class RecipeSearchReader(ApplicationDbContext dbContext) : IRecipeSearchReader
{
    public async Task<PagedResult<RecipeSearchResult>> SearchAsync(
        RecipeSearchOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        options = options.Validate();

        RecipeDifficulty? difficulty = null;
        if (!string.IsNullOrWhiteSpace(options.Difficulty))
        {
            if (!Enum.TryParse<RecipeDifficulty>(options.Difficulty, true, out var parsedDifficulty) ||
                !Enum.IsDefined(parsedDifficulty))
            {
                throw new ArgumentException(
                    $"Unsupported recipe difficulty '{options.Difficulty}'.",
                    nameof(options));
            }

            difficulty = parsedDifficulty;
        }

        var where = BuildWhereClause(options, difficulty);
        var connection = dbContext.Database.GetDbConnection();
        var closeConnection = connection.State != ConnectionState.Open;
        if (closeConnection)
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            await using var countCommand = connection.CreateCommand();
            countCommand.CommandText = $"SELECT COUNT(*) FROM \"Recipes\" AS r WHERE {where}";
            SetCurrentTransaction(countCommand);
            AddFilterParameters(countCommand, options, difficulty);
            var totalCount = checked((int)Convert.ToInt64(
                await countCommand.ExecuteScalarAsync(cancellationToken),
                System.Globalization.CultureInfo.InvariantCulture));

            await using var queryCommand = connection.CreateCommand();
            queryCommand.CommandText = $"""
                SELECT r."Id",
                       r."Slug",
                       r."Title",
                       r."Description",
                       r."CategoryId",
                       r."Difficulty",
                       r."CookTimeMinutes",
                       COALESCE(r."PublishedAt", r."CreatedAt"),
                       {GetRelevanceExpression(options.SearchTerm)} AS "RelevanceScore"
                FROM "Recipes" AS r
                WHERE {where}
                ORDER BY {GetOrderBy(options.Sort)}
                LIMIT @pageSize
                OFFSET @offset
                """;
            SetCurrentTransaction(queryCommand);
            AddFilterParameters(queryCommand, options, difficulty);
            queryCommand.Parameters.Add(new NpgsqlParameter("pageSize", NpgsqlDbType.Integer)
            {
                Value = options.PageSize,
            });
            queryCommand.Parameters.Add(new NpgsqlParameter("offset", NpgsqlDbType.Bigint)
            {
                Value = (long)(options.Page - 1) * options.PageSize,
            });

            var results = new List<RecipeSearchResult>(options.PageSize);
            await using var reader = await queryCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new RecipeSearchResult(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetGuid(4),
                    reader.GetString(5),
                    reader.GetInt32(6),
                    reader.GetFieldValue<DateTimeOffset>(7),
                    reader.IsDBNull(8) ? null : reader.GetFloat(8)));
            }

            return PagedResult.Create(results, totalCount, options.Page, options.PageSize);
        }
        finally
        {
            if (closeConnection)
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }
    }

    private void SetCurrentTransaction(System.Data.Common.DbCommand command)
    {
        if (dbContext.Database.CurrentTransaction is { } transaction)
        {
            command.Transaction = transaction.GetDbTransaction();
        }
    }

    private static string BuildWhereClause(RecipeSearchOptions options, RecipeDifficulty? difficulty)
    {
        var conditions = new List<string>
        {
            "r.\"Status\" = 'Published'",
            "r.\"IsDeleted\" = FALSE",
        };

        if (options.SearchTerm is not null)
        {
            conditions.Add(
                "(r.\"SearchVector\" @@ websearch_to_tsquery('culinary_vietnamese', @searchTerm) " +
                "OR r.\"Title\" % @searchTerm)");
        }

        if (options.CategoryId.HasValue)
        {
            conditions.Add("r.\"CategoryId\" = @categoryId");
        }

        if (difficulty.HasValue)
        {
            conditions.Add("r.\"Difficulty\" = @difficulty");
        }

        if (options.MaxCookTime.HasValue)
        {
            conditions.Add("r.\"CookTimeMinutes\" <= @maxCookTime");
        }

        if (options.MinServings.HasValue)
        {
            conditions.Add("r.\"Servings\" >= @minServings");
        }

        return string.Join(" AND ", conditions);
    }

    private static void AddFilterParameters(
        System.Data.Common.DbCommand command,
        RecipeSearchOptions options,
        RecipeDifficulty? difficulty)
    {
        if (options.SearchTerm is not null)
        {
            command.Parameters.Add(new NpgsqlParameter("searchTerm", NpgsqlDbType.Text)
            {
                Value = options.SearchTerm,
            });
        }

        if (options.CategoryId is { } categoryId)
        {
            command.Parameters.Add(new NpgsqlParameter("categoryId", NpgsqlDbType.Uuid)
            {
                Value = categoryId,
            });
        }

        if (difficulty.HasValue)
        {
            command.Parameters.Add(new NpgsqlParameter("difficulty", NpgsqlDbType.Text)
            {
                Value = difficulty.Value.ToString(),
            });
        }

        if (options.MaxCookTime is { } maxCookTime)
        {
            command.Parameters.Add(new NpgsqlParameter("maxCookTime", NpgsqlDbType.Integer)
            {
                Value = maxCookTime,
            });
        }

        if (options.MinServings is { } minServings)
        {
            command.Parameters.Add(new NpgsqlParameter("minServings", NpgsqlDbType.Integer)
            {
                Value = minServings,
            });
        }
    }

    private static string GetRelevanceExpression(string? searchTerm) =>
        searchTerm is null
            ? "NULL::real"
            : """
              GREATEST(
                  ts_rank_cd(
                      r."SearchVector",
                      websearch_to_tsquery('culinary_vietnamese', @searchTerm)),
                  similarity(r."Title", @searchTerm))
              """;

    private static string GetOrderBy(string sort)
    {
        var descending = sort.StartsWith('-');
        var direction = descending ? "DESC" : "ASC";
        var sortField = descending ? sort[1..] : sort;

        return sortField switch
        {
            "createdAt" => $"r.\"CreatedAt\" {direction}, r.\"Id\" ASC",
            "title" => $"r.\"Title\" {direction}, r.\"Id\" ASC",
            "cookTime" => $"r.\"CookTimeMinutes\" {direction}, r.\"Id\" ASC",
            "relevance" =>
                $"\"RelevanceScore\" {direction}, r.\"CreatedAt\" DESC, r.\"Id\" ASC",
            _ => throw new ArgumentException($"Unsupported sort field '{sort}'.", nameof(sort)),
        };
    }
}
