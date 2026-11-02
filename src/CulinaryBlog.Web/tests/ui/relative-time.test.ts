import assert from "node:assert/strict";
import test from "node:test";
import { relativeTimeVi } from "../../lib/ui/format.ts";

const now = new Date("2026-11-02T12:00:00Z");
const ago = (minutes: number) => new Date(now.getTime() - minutes * 60_000).toISOString();

test("relative time follows the steps used on the dashboard boards", () => {
  assert.equal(relativeTimeVi(ago(0), now), "Vừa xong");
  assert.equal(relativeTimeVi(ago(5), now), "5 phút trước");
  assert.equal(relativeTimeVi(ago(120), now), "2 giờ trước");
  assert.equal(relativeTimeVi(ago(26 * 60), now), "Hôm qua");
  assert.equal(relativeTimeVi(ago(3 * 24 * 60), now), "3 ngày trước");
  assert.equal(relativeTimeVi(ago(14 * 24 * 60), now), "2 tuần trước");
  assert.equal(relativeTimeVi(ago(60 * 24 * 60), now), "2 tháng trước");
});

test("anything over a year shows the date, and a clock ahead of the server is not negative", () => {
  assert.equal(relativeTimeVi("2024-01-15T05:00:00Z", now), "15/01/2024");
  assert.equal(relativeTimeVi(ago(-5), now), "Vừa xong");
});
