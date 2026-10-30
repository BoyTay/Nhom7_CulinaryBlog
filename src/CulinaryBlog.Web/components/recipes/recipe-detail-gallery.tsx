"use client";

import { useState } from "react";
import type { RecipeDetailImageModel } from "@/lib/recipes/detail";
import { RecipeImage } from "./recipe-image";

const VISIBLE_THUMBNAILS = 3;

export function RecipeDetailGallery({
  images,
  title,
}: {
  images: RecipeDetailImageModel[];
  title: string;
}) {
  const [selectedIndex, setSelectedIndex] = useState(0);
  const selected = images[selectedIndex];
  const hidden = images.length - VISIBLE_THUMBNAILS;

  return (
    <section className="recipe-detail-gallery" aria-label="Hình ảnh món ăn">
      <div className="recipe-detail-main-image">
        <RecipeImage
          alt={selected?.alt || `Chưa có ảnh minh họa cho ${title}`}
          className="dish kb ph recipe-detail-main-image-visual"
          height={500}
          image={selected?.image}
          key={selected?.id || "recipe-placeholder"}
          priority
          sizes="(max-width: 767px) 100vw, 880px"
          variant="original"
          width={800}
        />
        {selected?.isPrimary && <span className="recipe-detail-primary-label">Ảnh chính · {title}</span>}
        {images.length > 1 && (
          <div className="recipe-detail-dots" role="group" aria-label={`Ảnh ${selectedIndex + 1} trên ${images.length}`}>
            {images.map((image, index) => (
              <button
                type="button"
                aria-label={`Xem ảnh ${index + 1}`}
                aria-pressed={index === selectedIndex}
                key={image.id}
                onClick={() => setSelectedIndex(index)}
              />
            ))}
          </div>
        )}
      </div>
      {images.length > 1 && (
        <div className="recipe-detail-thumbnails" role="group" aria-label="Chọn ảnh công thức">
          {images.slice(0, VISIBLE_THUMBNAILS).map((image, index) => (
            <button
              type="button"
              aria-label={`Xem ảnh ${index + 1}: ${image.alt}`}
              aria-pressed={index === selectedIndex}
              className="recipe-detail-thumbnail"
              key={image.id}
              onClick={() => setSelectedIndex(index)}
            >
              <RecipeImage alt="" className="ph recipe-detail-thumbnail-visual" height={150} image={image.image} sizes="140px" variant="thumbnail" width={240} />
            </button>
          ))}
          {hidden > 0 && (
            <button
              type="button"
              className="recipe-detail-more"
              onClick={() => setSelectedIndex((selectedIndex + 1) % images.length)}
            >
              +{hidden} ảnh
            </button>
          )}
        </div>
      )}
    </section>
  );
}
