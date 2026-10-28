import assert from "node:assert/strict";
import { readdirSync, readFileSync, statSync } from "node:fs";
import { join, relative, sep } from "node:path";
import test from "node:test";
import { routes } from "../../lib/navigation/routes.ts";

const webRoot = join(import.meta.dirname, "..", "..");

function walk(dir: string, accept: (file: string) => boolean): string[] {
  return readdirSync(dir).flatMap((name) => {
    const full = join(dir, name);
    if (name === "node_modules" || name === ".next") return [];
    if (statSync(full).isDirectory()) return walk(full, accept);
    return accept(full) ? [full] : [];
  });
}

function pagePatterns(): RegExp[] {
  const appDir = join(webRoot, "app");
  return walk(appDir, (file) => file.endsWith(`${sep}page.tsx`)).map((file) => {
    const segments = relative(appDir, file)
      .split(sep)
      .slice(0, -1)
      .filter((segment) => !(segment.startsWith("(") && segment.endsWith(")")))
      .map((segment) => (segment.startsWith("[") ? "[^/]+" : segment));
    return new RegExp(`^/${segments.join("/")}$`);
  });
}

function routeResolves(path: string, patterns: RegExp[]): boolean {
  return patterns.some((pattern) => pattern.test(path));
}

const pending = { todo: "pages arrive in later tasks of the UX cohesion plan" };

test("every internal href in source resolves to an existing page", pending, () => {
  const patterns = pagePatterns();
  const files = [
    ...walk(join(webRoot, "app"), (f) => /\.tsx$/.test(f)),
    ...walk(join(webRoot, "components"), (f) => /\.tsx$/.test(f)),
  ];
  const literal = /(?:href=|router\.(?:push|replace)\()\s*"(\/[^"]*)"/g;
  const missing: string[] = [];
  let checked = 0;

  for (const file of files) {
    const source = readFileSync(file, "utf8");
    for (const match of source.matchAll(literal)) {
      const path = match[1].split(/[?#]/)[0] || "/";
      checked += 1;
      if (path === "/scalar" || path.startsWith("/api/")) continue;
      if (!routeResolves(path, patterns)) missing.push(`${relative(webRoot, file)} -> ${path}`);
    }
  }

  assert.ok(checked > 0, "expected to find internal links to verify");
  assert.deepEqual(missing, []);
});

test("route registry only lists existing pages", pending, () => {
  const patterns = pagePatterns();
  const missing = Object.entries(routes)
    .filter(([, path]) => !routeResolves(path, patterns))
    .map(([key, path]) => `${key} -> ${path}`);

  assert.deepEqual(missing, []);
});
