import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { Alert } from "@/components/shared";
import { QueryPagination } from "@/components/shared/query-pagination";
import { ApiError } from "@/lib/api/client";
import { buildCategoryDetailView, parseCategoryPage } from "@/lib/categories/category-view";
import { getCategoryDetail } from "@/lib/categories/server-categories";
import { routes } from "@/lib/navigation/routes";
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

  return (
    <div className="recipes-page">
      <main className="recipe-list-main">
        <nav className="recipe-detail-breadcrumb" aria-label="Breadcrumb">
          <Link href={routes.categories}>Danh mục</Link>
          <span aria-hidden="true">/</span>
          <span aria-current="page">{view.title}</span>
        </nav>
        <header className="recipe-list-heading">
          <h1>{view.title}</h1>
          <p>{view.description ?? `${view.totalCount} công thức`}</p>
        </header>
        {view.state === "empty" ? (
          <section className="recipe-list-empty" aria-live="polite">
            <h2>Danh mục này chưa có công thức</h2>
            <p><Link href={routes.recipes}>Xem tất cả công thức</Link></p>
          </section>
        ) : (
          <section className="recipe-list-grid" aria-label={`Công thức trong ${view.title}`}>
            {view.cards.map((card) => (
              <Link className="recipe-list-card" href={card.href} key={card.id}>
                <div className="recipe-list-card-body">
                  <h2>{card.title}</h2>
                  {card.description && <p>{card.description}</p>}
                </div>
              </Link>
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
