using CulinaryBlog.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

public sealed class RecipeImageConfiguration : IEntityTypeConfiguration<RecipeImage>
{
    public void Configure(EntityTypeBuilder<RecipeImage> builder)
    {
        builder.ToTable("RecipeImages");
        builder.HasKey(image => image.Id);
        builder.Property<Guid>("RecipeId").IsRequired();
        builder.Ignore(image => image.Url);
        builder.Property(image => image.OriginalUrl).HasMaxLength(2048).IsRequired();
        builder.Property(image => image.MediumUrl).HasMaxLength(2048);
        builder.Property(image => image.ThumbnailUrl).HasMaxLength(2048);
        builder.Property(image => image.AltText).HasMaxLength(300);
        builder.Property(image => image.OrderIndex).IsRequired();
        builder.HasIndex("RecipeId", nameof(RecipeImage.IsPrimary))
            .IsUnique()
            .HasFilter("\"IsPrimary\" = true");
        builder.HasIndex("RecipeId", nameof(RecipeImage.OrderIndex)).IsUnique();
    }
}
