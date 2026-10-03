using CulinaryBlog.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Name).HasMaxLength(100).IsRequired();
        builder.Property(category => category.Slug).HasMaxLength(120).IsRequired();
        builder.Property(category => category.Description).HasMaxLength(2000);
        builder.Property(category => category.ImageUrl).HasMaxLength(500);
        builder.Property(category => category.OrderIndex).HasDefaultValue(0);
        builder.Property(category => category.Version).HasColumnName("xmin").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
        builder.HasIndex(category => category.Name).IsUnique();
        builder.HasIndex(category => category.Slug).IsUnique();
        builder.HasIndex(category => new { category.OrderIndex, category.Name });
        builder.HasQueryFilter(category => !category.IsDeleted);
    }
}
