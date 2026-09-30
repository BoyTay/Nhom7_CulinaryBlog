using CulinaryBlog.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence.Seeders;

public sealed class CategorySeeder(ApplicationDbContext dbContext)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Categories.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.Categories.AddRange(
            Category.Create("Món chính", "mon-chinh", orderIndex: 0),
            Category.Create("Món khai vị", "mon-khai-vi", orderIndex: 1),
            Category.Create("Tráng miệng", "trang-mieng", orderIndex: 2));
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
