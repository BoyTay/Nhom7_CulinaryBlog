using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Common.Exceptions;
using CulinaryBlog.Domain.Categories;
using FluentValidation;
using Microsoft.Extensions.Caching.Memory;

namespace CulinaryBlog.Application.Categories;

public sealed record CreateCategoryCommand(string Name, string? Description, string? ImageUrl, int OrderIndex)
    : ICommand<CategoryDto>;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        CategoryValidation.AddNameRules(RuleFor(command => command.Name));
        CategoryValidation.AddOptionalRules(this, command => command.Description, command => command.ImageUrl, command => command.OrderIndex);
    }
}

public sealed class CreateCategoryCommandHandler(
    ICategoryRepository categories, IDataSession session, IMemoryCache cache)
    : ICommandHandler<CreateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await categories.NameExistsAsync(name, cancellationToken: cancellationToken))
        {
            throw new ConflictException("Category name already exists.", "CATEGORY_NAME_EXISTS");
        }

        var baseSlug = CategorySlug.Generate(name);
        var slug = baseSlug;
        for (var suffix = 2; await categories.SlugExistsAsync(slug, cancellationToken); suffix++)
        {
            var tail = $"-{suffix}";
            slug = baseSlug[..Math.Min(baseSlug.Length, 120 - tail.Length)] + tail;
        }

        var category = Category.Create(name, slug, request.Description, request.ImageUrl, request.OrderIndex);
        categories.Add(category);
        await session.SaveChangesAsync(cancellationToken);
        cache.Remove(CategoryQueries.CacheKey);
        return CategoryDto.From(category);
    }
}

public sealed record UpdateCategoryCommand(Guid Id, string Name, string? Description, string? ImageUrl, int OrderIndex)
    : ICommand<CategoryDto>;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        CategoryValidation.AddNameRules(RuleFor(command => command.Name));
        CategoryValidation.AddOptionalRules(this, command => command.Description, command => command.ImageUrl, command => command.OrderIndex);
    }
}

public sealed class UpdateCategoryCommandHandler(
    ICategoryRepository categories, IDataSession session, IMemoryCache cache)
    : ICommandHandler<UpdateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Category not found.", "CATEGORY_NOT_FOUND");
        var name = request.Name.Trim();
        if (await categories.NameExistsAsync(name, category.Id, cancellationToken))
        {
            throw new ConflictException("Category name already exists.", "CATEGORY_NAME_EXISTS");
        }

        category.Update(name, request.Description, request.ImageUrl, request.OrderIndex);
        await session.SaveChangesAsync(cancellationToken);
        cache.Remove(CategoryQueries.CacheKey);
        var recipeCount = await categories.CountRecipesAsync(category.Id, true, cancellationToken);
        return CategoryDto.From(category, recipeCount);
    }
}

public sealed record DeleteCategoryCommand(Guid Id) : ICommand;

public sealed class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}

public sealed class DeleteCategoryCommandHandler(
    ICategoryRepository categories, IDataSession session, IMemoryCache cache, TimeProvider timeProvider)
    : ICommandHandler<DeleteCategoryCommand>
{
    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Category not found.", "CATEGORY_NOT_FOUND");
        var recipeCount = await categories.CountRecipesAsync(category.Id, false, cancellationToken);
        if (recipeCount > 0)
        {
            throw new ConflictException($"Category contains {recipeCount} recipes.", "CATEGORY_DELETE_HAS_RECIPES");
        }

        category.Delete(timeProvider.GetUtcNow());
        await session.SaveChangesAsync(cancellationToken);
        cache.Remove(CategoryQueries.CacheKey);
    }
}
