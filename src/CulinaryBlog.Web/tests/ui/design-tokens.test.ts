import assert from "node:assert/strict";
import { readdirSync, readFileSync, statSync } from "node:fs";
import { join, relative } from "node:path";
import test from "node:test";

const webRoot = join(import.meta.dirname, "..", "..");

function cssFiles(dir: string): string[] {
  return readdirSync(dir).flatMap((name) => {
    const full = join(dir, name);
    if (name === "node_modules" || name === ".next") return [];
    if (statSync(full).isDirectory()) return cssFiles(full);
    return full.endsWith(".css") ? [full] : [];
  });
}

const files = [...cssFiles(join(webRoot, "app")), ...cssFiles(join(webRoot, "components"))];

test("no stylesheet keeps the scaffold's purple palette or Atkinson font", () => {
  const offenders = files.flatMap((file) => {
    const source = readFileSync(file, "utf8").toLowerCase();
    return ["#7c3aed", "#faf5ff", "#35136d", "atkinson"].filter((needle) => source.includes(needle)).map((needle) => `${relative(webRoot, file)}: ${needle}`);
  });
  assert.deepEqual(offenders, []);
});

test("globals.css declares the design palette on :root", () => {
  const source = readFileSync(join(webRoot, "app", "globals.css"), "utf8").toLowerCase();
  const root = source.slice(source.indexOf(":root"), source.indexOf("}", source.indexOf(":root")));
  const expected = {
    "--ds-bg": "#faf6ee",
    "--ds-surface": "#fffdf8",
    "--ds-ink": "#2a1d15",
    "--ds-muted": "#6b5a4c",
    "--ds-line": "#e8decb",
    "--ds-line-strong": "#c9bba2",
    "--ds-primary": "#c2410c",
    "--ds-primary-tint": "#fce9dd",
    "--ds-primary-dark-text": "#9a3412",
    "--ds-danger": "#b42318",
    "--ds-danger-tint": "#fde8e4",
    "--ds-warn-tint": "#fbefcb",
    "--ds-success-tint": "#d6eadb",
  };
  for (const [name, value] of Object.entries(expected)) {
    assert.ok(root.includes(`${name}: ${value};`), name);
  }
});

test("fonts load through next/font, not a render-blocking @import", () => {
  const offenders = files.filter((file) => /@import\s+url\(["']?https:\/\/fonts\.googleapis\.com/i.test(readFileSync(file, "utf8")));
  assert.deepEqual(offenders.map((file) => relative(webRoot, file)), []);
});
