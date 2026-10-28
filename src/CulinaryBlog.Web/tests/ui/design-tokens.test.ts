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
    "--ds-bg": "#f6f5f0",
    "--ds-surface": "#ffffff",
    "--ds-ink": "#1c2118",
    "--ds-muted": "#5b6356",
    "--ds-line": "#e2e0d6",
    "--ds-line-strong": "#b9b6a8",
    "--ds-primary": "#2d5d43",
    "--ds-primary-tint": "#e4eee7",
    "--ds-primary-dark-text": "#1f4630",
    "--ds-danger": "#a8321c",
    "--ds-danger-tint": "#fbe9e5",
    "--ds-warn-tint": "#f3e6bf",
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
