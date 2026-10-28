export const routes = {
  home: "/",
  recipes: "/recipes",
  search: "/search",
  categories: "/categories",
  login: "/login",
  register: "/register",
  profile: "/profile",
  dashboard: "/dashboard",
  dashboardRecipes: "/dashboard/recipes",
  dashboardRecipeNew: "/dashboard/recipes/new",
  dashboardCategories: "/dashboard/categories",
} as const;

export function recipeHref(slug: string): string {
  return `${routes.recipes}/${encodeURIComponent(slug)}`;
}

export function categoryHref(slug: string): string {
  return `${routes.categories}/${encodeURIComponent(slug)}`;
}

const staticPaths = new Set<string>(Object.values(routes));
const dynamicPatterns = [/^\/recipes\/[^/]+$/, /^\/categories\/[^/]+$/, /^\/dashboard\/recipes\/[^/]+\/edit$/];

export function isKnownRoute(path: string): boolean {
  const clean = path.split(/[?#]/)[0] || "/";
  return staticPaths.has(clean) || dynamicPatterns.some((pattern) => pattern.test(clean));
}
