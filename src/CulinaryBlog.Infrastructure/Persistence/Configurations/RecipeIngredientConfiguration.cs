using CulinaryBlog.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

public sealed class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient>
{
    public void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        builder.ToTable("RecipeIngredients");
        builder.HasKey(ingredient => ingredient.Id);
        builder.Property<Guid>("RecipeId").IsRequired();
        builder.Property(ingredient => ingredient.Name).HasMaxLength(200).IsRequired();
        builder.Property(ingredient => ingredient.Quantity).HasPrecision(12, 3).IsRequired();
        builder.Property(ingredient => ingredient.Unit).HasMaxLength(50).IsRequired();
        builder.Property(ingredient => ingredient.Notes).HasMaxLength(500);
        builder.HasIndex("RecipeId", nameof(RecipeIngredient.SortOrder)).IsUnique();
    }
}