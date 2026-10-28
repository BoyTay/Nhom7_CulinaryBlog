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
      <span className="category-card-strip" aria-hidden={imageUrl ? undefined : true}>
        {imageUrl && (
          // Category photos are stored as-is in MinIO; the browser loads them directly (see RecipeImage).
          <Image src={imageUrl} alt={`Ảnh danh mục ${name}`} fill sizes="280px" unoptimized className="category-card-image" />
        )}
      </span>
      <h2>{name}</h2>
      <p>{description?.trim() || "Chưa có mô tả."}</p>
      <span className="category-card-count">{recipeCount} công thức</span>
    </Link>
  );
}
