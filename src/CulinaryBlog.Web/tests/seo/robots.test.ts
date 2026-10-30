import assert from "node:assert/strict";
import test from "node:test";
import robots from "../../app/robots.ts";

test("robots allows public pages, hides private areas and declares the sitemap", () => {
  const result = robots();
  const rule = Array.isArray(result.rules) ? result.rules[0] : result.rules;

  assert.equal(rule.userAgent, "*");
  assert.equal(rule.allow, "/");
  assert.deepEqual(rule.disallow, ["/dashboard", "/profile", "/api/"]);
  assert.equal(result.sitemap, "http://localhost/sitemap.xml");
});

test("robots sitemap follows NEXT_PUBLIC_SITE_URL without a trailing slash", () => {
  process.env.NEXT_PUBLIC_SITE_URL = "https://culinary.example/";
  try {
    assert.equal(robots().sitemap, "https://culinary.example/sitemap.xml");
  } finally {
    delete process.env.NEXT_PUBLIC_SITE_URL;
  }
});
