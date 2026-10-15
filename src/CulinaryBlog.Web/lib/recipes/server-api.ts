import "server-only";

export function recipeServerApiBaseUrl(): string {
  const defaultTarget = process.env.NODE_ENV === "production"
    ? "http://api:8080"
    : "http://localhost:8080";
  const target = (process.env.API_PROXY_TARGET ?? defaultTarget).replace(/\/$/, "");
  return target.endsWith("/api/v1") ? target : `${target}/api/v1`;
}
