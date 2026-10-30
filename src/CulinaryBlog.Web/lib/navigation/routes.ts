export const routes = {
  home: "/",
  recipes: "/recipes",
  search: "/search",
  categories: "/categories",
  login: "/login",
  register: "/register",
  forgotPassword: "/forgot-password",
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

/** Public detail URL for a recipe. Pages are addressed by slug; ids only exist for legacy redirects. */
export function recipeDetailHref(recipe: { id: string; slug: string }): string {
  return recipeHref(recipe.slug);
}

const staticPaths = new Set<string>(Object.values(routes));
const dynamicPatterns = [/^\/recipes\/[^/]+$/, /^\/categories\/[^/]+$/, /^\/dashboard\/recipes\/[^/]+\/edit$/];

export function isKnownRoute(path: string): boolean {
  const clean = path.split(/[?#]/)[0] || "/";
  return staticPaths.has(clean) || dynamicPatterns.some((pattern) => pattern.test(clean));
}

export interface NavItem {
  label: string;
  href: string;
  requiresRole?: "Author" | "Admin";
}

const navItems: readonly NavItem[] = [
  { label: "Công thức", href: routes.recipes },
  { label: "Danh mục", href: routes.categories },
  { label: "Tìm kiếm", href: routes.search },
  { label: "Bảng điều khiển", href: routes.dashboard, requiresRole: "Author" },
];

export function buildNavItems(user: { roles: string[] } | null): NavItem[] {
  const roles = new Set((user?.roles ?? []).map((role) => role.toLowerCase()));
  const isAdmin = roles.has("admin");
  const isAuthor = isAdmin || roles.has("author");

  return navItems.filter((item) => {
    if (item.requiresRole === "Admin") return isAdmin;
    if (item.requiresRole === "Author") return isAuthor;
    return true;
  });
}

const chromelessPatterns = [
  /^\/dashboard(\/.*)?$/,
  /^\/(login|register|forgot-password|reset-password)$/,
  /^\/dashboard\/recipes\/new$/,
  /^\/dashboard\/recipes\/[^/]+\/edit$/,
];

export function showsSiteChrome(pathname: string): boolean {
  return !chromelessPatterns.some((pattern) => pattern.test(pathname));
}

export function isActiveNav(pathname: string, href: string): boolean {
  if (href === routes.home) return pathname === routes.home;
  return pathname === href || pathname.startsWith(`${href}/`);
}
