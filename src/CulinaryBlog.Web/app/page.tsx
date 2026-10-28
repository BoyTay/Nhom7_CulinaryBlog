import Link from "next/link";
import { HomeCta } from "@/components/home/home-cta";
import { RecipeCard } from "@/components/recipes/recipe-card";
import { RecipeImage } from "@/components/recipes/recipe-image";
import { getHomeData } from "@/lib/home/server-home";
import { routes } from "@/lib/navigation/routes";
import "./home.css";

export const dynamic = "force-dynamic";

export default async function HomePage() {
  const home = await getHomeData();
  const recipesUnavailable =
    home.state === "error" || (home.state === "partial" && home.featured.length === 0 && home.categories.length > 0);
  const heroImage = home.hero?.thumbnailUrl
    ? {
        thumbnailUrl: home.hero.thumbnailUrl,
        mediumUrl: null,
        originalUrl: home.hero.thumbnailUrl,
        url: home.hero.thumbnailUrl,
      }
    : null;

  return (
    <main>
      <section className="home-hero">
        <div className="home-hero-copy">
          <h1>Món ngon từ bếp nhà, chia sẻ cho mọi người.</h1>
          <p>Khám phá công thức từ nhiều nền ẩm thực, tìm theo nguyên liệu không dấu, và đăng công thức của riêng bạn.</p>
          <div className="home-hero-actions">
            <Link className="btn btn-primary btn-lg" href={routes.recipes}>Khám phá công thức</Link>
            <Link className="btn btn-outline btn-lg" href={routes.dashboardRecipeNew}>Đăng bài của bạn</Link>
          </div>
        </div>
        <div className="home-hero-visual">
          {home.hero ? (
            <Link href={home.hero.href} className="home-hero-link" aria-label={`Xem công thức ${home.hero.title}`}>
              <RecipeImage
                alt={home.hero.thumbnailAlt}
                className="home-hero-image"
                height={420}
                image={heroImage}
                priority
                variant="thumbnail"
                width={520}
              />
              <span className="home-hero-tag">{home.hero.title}</span>
            </Link>
          ) : (
            <span className="home-hero-tag">Ảnh công thức nổi bật</span>
          )}
        </div>
      </section>

      <section className="home-section" aria-labelledby="latest-title">
        <div className="home-section-head">
          <h2 id="latest-title">Công thức mới nhất</h2>
          <Link href={routes.recipes}>Xem tất cả</Link>
        </div>
        {recipesUnavailable ? (
          <div className="empty-state" role="status">
            <p>
              Chưa thể tải danh sách công thức lúc này. Bạn vẫn có thể{" "}
              <Link href={routes.recipes}>xem tất cả công thức</Link>.
            </p>
          </div>
        ) : home.featured.length === 0 && !home.hero ? (
          <div className="empty-state">
            <h2>Chưa có công thức nào được xuất bản</h2>
            <p>Hãy là người đầu tiên chia sẻ món ngon của bạn.</p>
          </div>
        ) : (
          <div className="home-grid">
            {home.featured.map((card) => (
              <RecipeCard card={card} key={card.id} />
            ))}
          </div>
        )}
      </section>

      {home.categories.length > 0 && (
        <section className="home-categories" aria-labelledby="categories-title">
          <div className="home-categories-inner">
            <h2 id="categories-title">Duyệt theo danh mục</h2>
            <ul>
              {home.categories.map((category) => (
                <li key={category.href}>
                  <Link href={category.href}>
                    {category.name}
                    <span>{category.recipeCount}</span>
                  </Link>
                </li>
              ))}
            </ul>
          </div>
        </section>
      )}

      <HomeCta />
    </main>
  );
}
