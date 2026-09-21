using CulinaryBlog.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

public sealed class RecipeStepConfiguration : IEntityTypeConfiguration<RecipeStep>
{
    public void Configure(EntityTypeBuilder<RecipeStep> builder)
    {
        builder.ToTable("RecipeSteps");
        builder.HasKey(step => step.Id);
        builder.Property<Guid>("RecipeId").IsRequired();
        builder.Property(step => step.Description).HasMaxLength(2000).IsRequired();
        builder.Property(step => step.ImageUrl).HasMaxLength(2048);
        builder.HasIndex("RecipeId", nameof(RecipeStep.StepNumber)).IsUnique();
    }
}