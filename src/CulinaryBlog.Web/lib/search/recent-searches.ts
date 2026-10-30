const STORAGE_KEY = "culinary-blog-recent-searches";
const MAX_RECENT = 3;

/** Newest first, without repeats. Kept apart from storage so the rule is easy to test. */
export function addRecentSearch(recent: readonly string[], term: string): string[] {
  const key = term.toLocaleLowerCase("vi");
  return [term, ...recent.filter((item) => item.toLocaleLowerCase("vi") !== key)].slice(0, MAX_RECENT);
}

// Storage can be blocked or full (private mode); search keeps working without the list.
export function readRecentSearches(): string[] {
  try {
    const stored: unknown = JSON.parse(window.localStorage.getItem(STORAGE_KEY) ?? "[]");
    return Array.isArray(stored) ? stored.filter((item) => typeof item === "string").slice(0, MAX_RECENT) : [];
  } catch {
    return [];
  }
}

export function rememberSearch(term: string): string[] {
  const next = addRecentSearch(readRecentSearches(), term);
  try {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
  } catch {
    // Not saved; the list still shows for this visit.
  }
  return next;
}
