import assert from "node:assert/strict";
import test from "node:test";
import { searchSummary } from "../../lib/search/search-summary.ts";

test("a keyword search titles the page with the keyword and mentions relevance ordering", () => {
  assert.deepEqual(searchSummary("pho bo", 3, "-relevance"), {
    title: "Kết quả cho “pho bo”",
    subtitle: "3 công thức, sắp xếp theo độ liên quan.",
  });
});

test("other sort orders do not claim relevance ordering", () => {
  assert.equal(searchSummary("pho bo", 3, "-createdAt").subtitle, "3 công thức.");
});

test("without a keyword the page lists all recipes", () => {
  assert.deepEqual(searchSummary("", 12, "-createdAt"), { title: "Tất cả công thức", subtitle: "12 công thức." });
});

test("no results suggests a shorter keyword and large counts use Vietnamese separators", () => {
  assert.equal(searchSummary("xyz", 0, "-relevance").subtitle, "Không có kết quả. Thử từ khóa ngắn hơn.");
  assert.equal(searchSummary("", 1234, "-createdAt").subtitle, "1.234 công thức.");
});
