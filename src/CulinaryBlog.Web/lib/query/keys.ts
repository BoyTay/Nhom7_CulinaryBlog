import type { RecipeSearchOptions } from "@/lib/api/contracts";
import type { UserFilters } from "@/lib/users/managed-users";

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
  users: {
    all: ["users"] as const,
    list: (filters: UserFilters) => ["users", filters] as const,
  },
} as const;
