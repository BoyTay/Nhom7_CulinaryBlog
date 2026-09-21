using CulinaryBlog.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

public sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable("Recipes");
        builder.HasKey(recipe => recipe.Id);

        builder.Property(recipe => recipe.Title)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(recipe => recipe.Slug)
            .HasMaxLength(220)
            .IsRequired();
        builder.Property(recipe => recipe.Description)
            .HasMaxLength(5000)
            .IsRequired();
        builder.Property(recipe => recipe.AuthorId)
            .HasMaxLength(450)
            .IsRequired();
        builder.Property(recipe => recipe.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(recipe => recipe.Difficulty)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(recipe => recipe.Version)
            .HasColumnName("xmin")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(recipe => recipe.Slug).IsUnique();
        builder.HasIndex(recipe => new { recipe.Status, recipe.CreatedAt });
        builder.HasIndex(recipe => recipe.CategoryId);
        builder.HasIndex(recipe => recipe.AuthorId);

        builder.HasQueryFilter(recipe => !recipe.IsDeleted);

        builder.OwnsOne(recipe => recipe.Nutrition, nutrition =>
        {
            nutrition.Property(value => value.Calories).HasColumnName("Calories");
            nutrition.Property(value => value.ProteinGrams).HasColumnName("ProteinGrams").HasPrecision(10, 2);
            nutrition.Property(value => value.CarbohydratesGrams).HasColumnName("CarbohydratesGrams").HasPrecision(10, 2);
            nutrition.Property(value => value.FatGrams).HasColumnName("FatGrams").HasPrecision(10, 2);
        });

        builder.HasMany(recipe => recipe.Steps)
            .WithOne()
            .HasForeignKey("RecipeId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(recipe => recipe.Ingredients)
            .WithOne()
            .HasForeignKey("RecipeId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(recipe => recipe.Images)
            .WithOne()
            .HasForeignKey("RecipeId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}