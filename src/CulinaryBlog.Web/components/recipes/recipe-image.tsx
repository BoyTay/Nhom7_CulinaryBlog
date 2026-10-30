"use client";

import Image from "next/image";
import { useState } from "react";
import {
  recipeImageSourceCandidates,
  recipeImageSourceForAttempt,
  type RecipeImageSource,
  type RecipeImageVariant,
} from "@/lib/recipes/image-display";

export function RecipeImage({
  alt,
  className,
  height,
  image,
  priority = false,
  sizes,
  variant = "medium",
  width,
}: {
  alt: string;
  className?: string;
  height: number;
  image?: RecipeImageSource | null;
  priority?: boolean;
  sizes?: string;
  variant?: RecipeImageVariant;
  width: number;
}) {
  const candidates = recipeImageSourceCandidates(image, variant);
  const sourceKey = candidates.join("\u0000");
  const [failure, setFailure] = useState({ sourceKey, index: 0 });
  const failedIndex = failure.sourceKey === sourceKey ? failure.index : 0;

  const { src: source, isPlaceholder } = recipeImageSourceForAttempt(candidates, failedIndex);

  return (
    <span
      className={className}
      data-placeholder={isPlaceholder || undefined}
      style={{
        aspectRatio: `${width} / ${height}`,
        display: "block",
        overflow: "hidden",
        position: "relative",
      }}
    >
      <Image
        alt={alt}
        fill
        onError={() => {
          if (!isPlaceholder) setFailure({ sourceKey, index: failedIndex + 1 });
        }}
        priority={priority}
        sizes={sizes ?? `${width}px`}
        src={source}
        // Uploaded photos already have thumbnail/medium variants. The Next optimizer runs inside the web
        // container, where the public MinIO host (localhost:9000) is not reachable, so let the browser load them.
        unoptimized={!isPlaceholder}
        style={{ objectFit: "cover" }}
      />
    </span>
  );
}
