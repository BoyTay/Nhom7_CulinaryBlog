import type { RecipeDifficulty } from "../recipes/contracts.ts";

const difficultyNames: Record<RecipeDifficulty, string> = {
  Easy: "Dễ",
  Medium: "Trung bình",
  Hard: "Khó",
};

export function difficultyLabel(difficulty: RecipeDifficulty): string {
  return difficultyNames[difficulty];
}

/** Minutes up to 119 ("90 phút"), hours from two hours ("2 giờ", "2 giờ 30 phút"), as in the design. */
export function cookTimeLabel(minutes: number): string {
  if (minutes < 120) return `${minutes} phút`;
  const hours = Math.floor(minutes / 60);
  const rest = minutes % 60;
  return rest === 0 ? `${hours} giờ` : `${hours} giờ ${rest} phút`;
}

const dateFormat = new Intl.DateTimeFormat("vi-VN", {
  timeZone: "Asia/Ho_Chi_Minh",
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
});

export function formatDateVi(iso: string): string {
  return dateFormat.format(new Date(iso));
}

export function initials(name: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean);
  if (words.length === 0) return "?";
  if (words.length === 1) return words[0][0].toUpperCase();
  return (words[0][0] + words[words.length - 1][0]).toUpperCase();
}
