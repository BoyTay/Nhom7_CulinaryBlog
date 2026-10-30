import assert from "node:assert/strict";
import test from "node:test";
import { cookTimeLabel, difficultyLabel, formatDateVi, initials } from "../../lib/ui/format.ts";

test("cookTimeLabel keeps minutes below two hours and switches to hours after", () => {
  assert.equal(cookTimeLabel(0), "0 phút");
  assert.equal(cookTimeLabel(30), "30 phút");
  assert.equal(cookTimeLabel(90), "90 phút");
  assert.equal(cookTimeLabel(120), "2 giờ");
  assert.equal(cookTimeLabel(150), "2 giờ 30 phút");
  assert.equal(cookTimeLabel(180), "3 giờ");
});

test("difficultyLabel uses the Vietnamese names from the design", () => {
  assert.equal(difficultyLabel("Easy"), "Dễ");
  assert.equal(difficultyLabel("Medium"), "Trung bình");
  assert.equal(difficultyLabel("Hard"), "Khó");
});

test("formatDateVi formats dd/MM/yyyy in Vietnam time", () => {
  assert.equal(formatDateVi("2026-10-14T03:00:00Z"), "14/10/2026");
  assert.equal(formatDateVi("2026-12-31T20:00:00Z"), "01/01/2027");
});

test("initials takes the first and last word, uppercased, with a fallback", () => {
  assert.equal(initials("Phan Trung Hiếu"), "PH");
  assert.equal(initials("an"), "A");
  assert.equal(initials("  "), "?");
});
