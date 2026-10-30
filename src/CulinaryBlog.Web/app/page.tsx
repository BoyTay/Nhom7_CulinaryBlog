import Image from "next/image";
import Link from "next/link";
import { HomeCta } from "@/components/home/home-cta";
import { RecipeCard } from "@/components/recipes/recipe-card";
import { getHomeData } from "@/lib/home/server-home";
import { routes } from "@/lib/navigation/routes";
import { buildSearchHref } from "@/lib/navigation/search-href";
import "./home.css";

export const dynamic = "force-dynamic";

const quickLinks = [
  { label: "Phở bò", href: buildSearchHref("phở bò") },
  { label: "Món chay", href: buildSearchHref("chay") },
  { label: "Dưới 30 phút", href: `${routes.recipes}?maxCook=30` },
  { label: "Dễ làm", href: `${routes.recipes}?difficulty=Easy` },
];

const categoryTints = [
  { tint: "#fce9dd", ink: "#9a3412" },
  { tint: "#fbefcb", ink: "#5c4306" },
  { tint: "#e3eedf", ink: "#1f4a2b" },
  { tint: "#f6dde0", ink: "#8a2a3a" },
  { tint: "#dde9f0", ink: "#1f4a63" },
  { tint: "#efe3d2", ink: "#5a4023" },
];

export default async function HomePage() {
  const home = await getHomeData();
  const recipesUnavailable =
    home.state === "error" || (home.state === "partial" && home.featured.length === 0 && home.categories.length > 0);

  return (
    <main>
      <section className="home-hero">
        <div className="home-hero-copy">
          <span className="rise home-eyebrow"><span aria-hidden="true" />Cộng đồng chia sẻ công thức Việt</span>
          <h1 className="rise">Bữa cơm ngon bắt đầu từ một công thức <span>đáng tin</span>.</h1>
          <p className="rise home-lede">Khám phá món ngon từ bếp nhà khắp ba miền, lọc theo độ khó và thời gian, rồi nấu theo từng bước có hẹn giờ.</p>
          <p className="rise home-lede-mobile">Lọc theo độ khó và thời gian, nấu theo từng bước có hẹn giờ.</p>
          <form role="search" action={routes.search} className="rise sf home-search">
            <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><circle cx="11" cy="11" r="7" /><path d="M20 20l-3.5-3.5" /></svg>
            <label htmlFor="hero-q" className="visually-hidden">Tìm công thức</label>
            <input id="hero-q" name="q" type="search" placeholder="Thử “bun cha”, “cá kho”, “món chay”…" />
            <button type="submit" className="btn btn-primary" aria-label="Tìm công thức"><span>Tìm</span><span className="home-search-word"> công thức</span></button>
          </form>
          <div className="home-hero-mobile">
            <Image className="dish pop ph" src="/images/recipes/pho.jpg" alt="" fill sizes="100vw" />
            {home.hero && (
              <Link href={home.hero.href}>
                <strong>{home.hero.title}</strong>
                {home.hero.cookTimeLabel} · {home.hero.difficultyLabel}
              </Link>
            )}
          </div>
          <div className="rise home-quick">
            <span>Gợi ý:</span>
            {quickLinks.map((link) => <Link href={link.href} className="home-chip" key={link.label}>{link.label}</Link>)}
          </div>
        </div>

        <div className="home-collage">
          <div className="pop home-collage-main">
            <Image className="dish ph" src="/images/recipes/pho.jpg" alt="" fill sizes="(max-width: 767px) 56vw, 300px" priority />
            <span>Ảnh: Phở bò</span>
          </div>
          <div className="pf home-collage-side">
            <Image className="ph" src="/images/recipes/goicuon.jpg" alt="" fill sizes="220px" />
            <span>Ảnh: Gỏi cuốn</span>
          </div>
          <div className="pf2 home-collage-round">
            <Image className="ph" src="/images/recipes/caphe.jpg" alt="" fill sizes="190px" />
          </div>
          {home.hero ? (
            <Link href={home.hero.href} className="pf3 home-collage-card">
              <span>Hôm nay nấu gì?</span>
              <strong>{home.hero.title}</strong>
              <small>{home.hero.cookTimeLabel} · {home.hero.difficultyLabel}</small>
            </Link>
          ) : (
            <Link href={routes.recipes} className="pf3 home-collage-card">
              <span>Hôm nay nấu gì?</span>
              <strong>Xem tất cả công thức</strong>
            </Link>
          )}
        </div>
      </section>

      {home.categories.length > 0 && (
        <section className="rv home-section" aria-labelledby="categories-title">
          <div className="home-section-head">
            <h2 id="categories-title"><span className="home-desktop-word">Duyệt theo danh mục</span><span className="home-mobile-word">Danh mục</span></h2>
            <Link href={routes.categories}>Tất cả danh mục →</Link>
          </div>
          <div className="home-categories">
            {home.categories.map((category, index) => {
              const { tint, ink } = categoryTints[index % categoryTints.length];
              return (
                <Link href={category.href} className="card home-category" key={category.href}>
                  <span className="home-category-icon" style={{ background: tint, color: ink }}>
                    <svg aria-hidden="true" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><path d="M7 3v8M4 3v5a3 3 0 0 0 6 0V3M7 11v10M17 3c-2 2-3 5-3 8h3v10" /></svg>
                  </span>
                  <span className="home-category-text">
                    <strong>{category.name}</strong>
                    <small>{category.recipeCount}<span className="home-desktop-word"> công thức</span></small>
                  </span>
                </Link>
              );
            })}
          </div>
        </section>
      )}

      <section className="rv home-section home-section-latest" aria-labelledby="latest-title">
        <div className="home-section-head">
          <h2 id="latest-title"><span className="home-desktop-word">Công thức mới nhất</span><span className="home-mobile-word">Mới nhất</span></h2>
          <Link href={routes.recipes}>Xem tất cả →</Link>
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

      <HomeCta />
    </main>
  );
}
