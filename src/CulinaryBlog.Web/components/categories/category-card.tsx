import Image from "next/image";
import Link from "next/link";
import "./category-card.css";

export function CategoryCard({
  name,
  href,
  description,
  imageUrl,
  recipeCount,
}: {
  name: string;
  href: string;
  description: string | null;
  imageUrl?: string | null;
  recipeCount: number;
}) {
  return (
    <Link className="card category-card" href={href}>
      <span className="category-card-photo">
        {imageUrl && (
          // Category photos are stored as-is in MinIO; the browser loads them directly (see RecipeImage).
          <Image src={imageUrl} alt={`Ảnh danh mục ${name}`} fill sizes="(max-width: 767px) 100vw, 380px" unoptimized className="dish ph category-card-image" />
        )}
        <span className="category-card-count">{recipeCount} công thức</span>
      </span>
      <span className="category-card-body">
        <h2>{name}</h2>
        <p>{description?.trim() || "Chưa có mô tả."}</p>
        <span className="category-card-more">
          Xem công thức
          <svg aria-hidden="true" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"><path d="M5 12h14M13 6l6 6-6 6" /></svg>
        </span>
      </span>
    </Link>
  );
}
