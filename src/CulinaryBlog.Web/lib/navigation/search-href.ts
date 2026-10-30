import { routes } from "./routes.ts";

const MIN_QUERY_LENGTH = 2;

export function buildSearchHref(query: string): string {
  const trimmed = query.trim();
  return trimmed.length < MIN_QUERY_LENGTH ? routes.search : `${routes.search}?q=${encodeURIComponent(trimmed)}`;
}
