import type { Metadata } from "next";
import Image from "next/image";
import Link from "next/link";
import { notFound } from "next/navigation";
import { RecipeCard } from "@/components/recipes/recipe-card";
import { Alert } from "@/components/shared";
import { QueryPagination } from "@/components/shared/query-pagination";
import { ApiError } from "@/lib/api/client";
import { buildCategoryDetailView, parseCategoryPage } from "@/lib/categories/category-view";
import { getCategoryDetail, getCategoryList } from "@/lib/categories/server-categories";
import { categoryHref, routes } from "@/lib/navigation/routes";
import "../../recipes/recipes.css";

type Props = {
  params: Promise<{ slug: string }>;
  searchParams: Promise<{ page?: string | string[] }>;
};

async function load(slug: string, rawPage: string | string[] | undefined) {
  const page = parseCategoryPage(typeof rawPage === "string" ? rawPage : undefined);
  try {
    return { detail: await getCategoryDetail(slug, page) };
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) notFound();
    return { detail: null };
  }
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { slug } = await params;
  const { detail } = await load(slug, undefined);
  if (!detail) return { title: "Danh mục" };
  const name = detail.category.name;
  return {
    title: { absolute: `${name} | Culinary Blog` },
    description: detail.category.description ?? `Công thức thuộc danh mục ${name}.`,
  };
}

export default async function CategoryDetailPage({ params, searchParams }: Props) {
  const { slug } = await params;
  const { detail } = await load(slug, (await searchParams).page);

  if (!detail) {
    return (
      <div className="recipes-page">
        <main className="recipe-list-main">
          <Alert variant="error" title="Chưa thể tải danh mục">Máy chủ đang gặp sự cố. Vui lòng thử lại sau.</Alert>
        </main>
      </div>
    );
  }

  const view = buildCategoryDetailView(detail.category, detail.recipes);
  const siblings = await getCategoryList().catch(() => []);

  return (
    <div className="recipes-page">
      <section className="category-hero" aria-labelledby="category-title">
        {detail.category.imageUrl && (
          <Image className="dish kb ph category-hero-image" src={detail.category.imageUrl} alt="" fill sizes="100vw" unoptimized priority />
        )}
        <div className="category-hero-shade" />
        <div className="category-hero-inner">
          <nav className="breadcrumb" aria-label="Đường dẫn">
            <Link href={routes.home}>Trang chủ</Link>
            <span aria-hidden="true">/</span>
            <Link href={routes.categories}>Danh mục</Link>
            <span aria-hidden="true">/</span>
            <span aria-current="page">{view.title}</span>
          </nav>
          <h1 id="category-title" className="rise">{view.title}</h1>
          <p>{view.subtitle}</p>
        </div>
      </section>
      <main className="category-detail-main">
        {siblings.length > 0 && (
          <div className="category-detail-tools">
            <nav aria-label="Các danh mục" className="category-chips">
              {siblings.map((category) => (
                <Link
                  href={categoryHref(category.slug)}
                  className="chip"
                  aria-current={category.slug === slug ? "page" : undefined}
                  key={category.id}
                >
                  {category.name} <small>{category.recipeCount}</small>
                </Link>
              ))}
            </nav>
          </div>
        )}
        {view.state === "empty" ? (
          <section className="empty-state" aria-live="polite">
            <h2>Danh mục này chưa có công thức</h2>
            <p><Link href={routes.recipes}>Xem tất cả công thức</Link></p>
          </section>
        ) : (
          <section className="category-detail-grid" aria-label={`Công thức trong ${view.title}`}>
            {view.cards.map((card) => (
              <RecipeCard card={card} key={card.id} />
            ))}
          </section>
        )}
        {view.totalPages > 1 && (
          <QueryPagination page={view.page} totalPages={view.totalPages} label="Phân trang công thức" />
        )}
      </main>
    </div>
  );
}
