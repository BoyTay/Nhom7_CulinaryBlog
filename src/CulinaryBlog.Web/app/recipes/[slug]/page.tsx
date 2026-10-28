import type { Metadata } from "next";
import { notFound, permanentRedirect } from "next/navigation";
import { RecipeDetail } from "@/components/recipes/recipe-detail";
import { recipeHref } from "@/lib/navigation/routes";
import {
  createRecipeDetailViewModel,
  createRecipeSeoModel,
  isPublicRecipeDetail,
  isRecipeId,
  isRecipeNotFoundError,
} from "@/lib/recipes/detail";
import { getPublicRecipeDetail, getPublicRecipeDetailBySlug } from "@/lib/recipes/server-detail";

export const revalidate = 300;
export const dynamicParams = true;

export function generateStaticParams(): [] {
  return [];
}

type Props = { params: Promise<{ slug: string }> };

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { slug } = await params;
  if (isRecipeId(slug)) return unavailableMetadata();

  try {
    const detail = await getPublicRecipeDetailBySlug(slug);
    if (!isPublicRecipeDetail(detail)) return unavailableMetadata();
    const seo = createRecipeSeoModel(detail);
    return {
      title: { absolute: seo.title },
      description: seo.description,
      robots: { index: true, follow: true },
      alternates: { canonical: recipeHref(detail.slug) },
      openGraph: {
        type: "article",
        title: seo.title,
        description: seo.description,
        ...(seo.imageUrl ? { images: [{ url: seo.imageUrl, alt: detail.title }] } : {}),
      },
    };
  } catch {
    return unavailableMetadata();
  }
}

export default async function RecipeDetailPage({ params }: Props) {
  const { slug } = await params;

  if (isRecipeId(slug)) {
    // Legacy `/recipes/{id}` links (old sitemap entries, saved bookmarks) go to the canonical slug URL.
    let legacy;
    try {
      legacy = await getPublicRecipeDetail(slug);
    } catch (error) {
      if (isRecipeNotFoundError(error)) notFound();
      throw error;
    }
    if (!isPublicRecipeDetail(legacy)) notFound();
    permanentRedirect(recipeHref(legacy.slug));
  }

  let detail;
  try {
    detail = await getPublicRecipeDetailBySlug(slug);
  } catch (error) {
    if (isRecipeNotFoundError(error)) notFound();
    throw error;
  }

  if (!isPublicRecipeDetail(detail)) notFound();

  const seo = createRecipeSeoModel(detail);
  const safeJsonLd = JSON.stringify(seo.jsonLd).replace(/</g, "\\u003c");

  return (
    <>
      <RecipeDetail recipe={createRecipeDetailViewModel(detail)} />
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd }} />
    </>
  );
}

function unavailableMetadata(): Metadata {
  return {
    title: { absolute: "Không tìm thấy công thức | Culinary Blog" },
    robots: { index: false, follow: false },
  };
}
