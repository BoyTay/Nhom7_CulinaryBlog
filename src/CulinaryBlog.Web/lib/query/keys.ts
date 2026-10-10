import type { RecipeSearchOptions } from "@/lib/api/contracts";

export const queryKeys = {
  categories: {
    all: ["categories"] as const,
    detail: (slug: string) => ["categories", slug] as const,
  },
  recipes: {
    all: ["recipes"] as const,
    detail: (slug: string) => ["recipes", slug] as const,
    search: (options: RecipeSearchOptions) => ["recipes", "search", options] as const,
  },
} as const;
