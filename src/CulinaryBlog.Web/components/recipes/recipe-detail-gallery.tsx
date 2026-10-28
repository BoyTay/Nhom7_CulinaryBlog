"use client";

import { useState } from "react";
import type { RecipeDetailImageModel } from "@/lib/recipes/detail";
import { RecipeImage } from "./recipe-image";

function RecipeVisual({
  alt,
  className,
  priority = false,
  image,
  variant = "original",
}: {
  alt: string;
  className: string;
  image?: RecipeDetailImageModel["image"];
  priority?: boolean;
  variant?: "thumbnail" | "medium" | "original";
}) {
  return (
    <RecipeImage
      alt={alt || "Chưa có ảnh minh họa cho công thức"}
      className={className}
      height={variant === "thumbnail" ? 64 : 440}
      image={image}
      priority={priority}
      sizes={variant === "thumbnail" ? "88px" : "(max-width: 767px) 100vw, 52vw"}
      variant={variant}
      width={variant === "thumbnail" ? 88 : 720}
    />
  );
}

export function RecipeDetailGallery({
  images,
  title,
}: {
  images: RecipeDetailImageModel[];
  title: string;
}) {
  const [selectedIndex, setSelectedIndex] = useState(0);
  const selected = images[selectedIndex];

  return (
    <section className="recipe-detail-gallery" aria-label="Hình ảnh công thức">
      <div className="recipe-detail-main-image">
        <RecipeVisual
          alt={selected?.alt || `Chưa có ảnh minh họa cho ${title}`}
          className="recipe-detail-main-image-visual"
          key={selected?.id || "recipe-placeholder"}
          priority
          image={selected?.image}
        />
        {selected?.isPrimary && <span className="recipe-detail-primary-label">Ảnh chính</span>}
      </div>
      {images.length > 1 && (
        <div className="recipe-detail-thumbnails" role="group" aria-label="Chọn ảnh công thức">
          {images.map((image, index) => (
            <button
              type="button"
              aria-label={`Xem ảnh ${index + 1}: ${image.alt}`}
              aria-pressed={index === selectedIndex}
              className="recipe-detail-thumbnail"
              data-selected={index === selectedIndex || undefined}
              key={image.id}
              onClick={() => setSelectedIndex(index)}
            >
              <RecipeVisual alt="" className="recipe-detail-thumbnail-visual" image={image.image} variant="thumbnail" />
            </button>
          ))}
        </div>
      )}
    </section>
  );
}
