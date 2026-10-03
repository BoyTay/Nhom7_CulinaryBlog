export const queryKeys = {
  categories: {
    all: ["categories"] as const,
    detail: (slug: string) => ["categories", slug] as const,
  },
  recipes: {
    all: ["recipes"] as const,
    detail: (slug: string) => ["recipes", slug] as const,
  },
} as const;
