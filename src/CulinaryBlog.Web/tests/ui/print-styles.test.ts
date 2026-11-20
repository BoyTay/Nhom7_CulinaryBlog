import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { join } from "node:path";
import test from "node:test";

const webRoot = join(import.meta.dirname, "..", "..");

function printBlocks(source: string): string {
  const blocks: string[] = [];
  for (const match of source.matchAll(/@media\s+print\s*\{/g)) {
    let depth = 1;
    let index = match.index + match[0].length;
    const start = index;
    while (depth > 0 && index < source.length) {
      if (source[index] === "{") depth++;
      if (source[index] === "}") depth--;
      index++;
    }
    blocks.push(source.slice(start, index - 1));
  }
  return blocks.join("\n");
}

// Entrance animations start from opacity 0. Scroll-driven ones (.rv) never run for content that was not scrolled
// into view, so without this the printed recipe lost its steps (and headless print lost every heading).
test("printing turns every entrance animation off", () => {
  const print = printBlocks(readFileSync(join(webRoot, "app", "motion.css"), "utf8"));

  assert.match(print, /\*\s*,\s*\*::before\s*,\s*\*::after\s*\{[^}]*animation\s*:\s*none\s*!important/);
});

// The recipe title lives in <header className="recipe-detail-header">; a bare `header` selector hid it on paper.
test("recipe print styles hide only the site chrome, not every header element", () => {
  const print = printBlocks(readFileSync(join(webRoot, "app", "recipes", "[slug]", "recipe-detail.css"), "utf8"));

  assert.doesNotMatch(print, /(^|[\s,{])(header|footer)\s*[,{]/);
  assert.match(print, /\.hd\b/);
});
