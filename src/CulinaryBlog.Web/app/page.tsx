import Link from "next/link";
import { HomeCta } from "@/components/home/home-cta";
import { RecipeCard } from "@/components/recipes/recipe-card";
import { getHomeData } from "@/lib/home/server-home";
import { routes } from "@/lib/navigation/routes";
import "./recipes/recipes.css";
import "./home.css";

export const revalidate = 3600;

export default async function HomePage() {
  const home = await getHomeData();

  return (
    <main>
      <section className="hero" id="top">
        <div className="hero-copy">
          <p className="kicker">Từ căn bếp đến cộng đồng</p>
          <h1>Mỗi món ăn đều có một câu chuyện đáng sẻ chia.</h1>
          <p className="lede">
            Một không gian gọn gàng để khám phá công thức mới, ghi lại bí quyết gia đình
            và truyền cảm hứng cho bữa ăn tiếp theo.
          </p>
          <form className="home-search" action={routes.search} method="get" role="search">
            <label className="visually-hidden" htmlFor="home-search-input">Tìm công thức</label>
            <input
              id="home-search-input"
              name="q"
              type="search"
              minLength={2}
              placeholder="Tìm theo tên món hoặc nguyên liệu"
              autoComplete="off"
            />
            <button className="button button-primary" type="submit">Tìm kiếm</button>
          </form>
        </div>
        {home.categories.length > 0 && (
          <div className="hero-panel" aria-label="Danh mục nổi bật">
            <p className="panel-label">Danh mục</p>
            <ul className="home-category-list">
              {home.categories.map((category) => (
                <li key={category.href}>
                  <Link href={category.href}>
                    <span>{category.name}</span>
                    <span>{category.recipeCount} món</span>
                  </Link>
                </li>
              ))}
            </ul>
          </div>
        )}
      </section>

      <section className="home-featured" aria-labelledby="featured-title">
        <div className="section-heading">
          <p className="kicker">Mới nhất</p>
          <h2 id="featured-title">Công thức vừa được chia sẻ</h2>
        </div>
        {home.state === "error" || (home.state === "partial" && home.featured.length === 0 && home.categories.length > 0) ? (
          <p className="home-note" role="status">
            Chưa thể tải danh sách công thức lúc này. Bạn vẫn có thể <Link href={routes.recipes}>xem tất cả công thức</Link>.
          </p>
        ) : home.featured.length === 0 ? (
          <p className="home-note">Chưa có công thức nào được xuất bản. Hãy là người đầu tiên chia sẻ.</p>
        ) : (
          <div className="recipe-list-grid">
            {home.featured.map((card) => <RecipeCard card={card} key={card.id} />)}
          </div>
        )}
        <p className="home-more"><Link href={routes.recipes}>Xem tất cả công thức →</Link></p>
      </section>

      <HomeCta />
    </main>
  );
}
