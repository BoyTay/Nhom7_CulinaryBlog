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

/** "2 giờ trước", "Hôm qua", "3 tuần trước"; anything over a year shows the date instead. */
export function relativeTimeVi(iso: string, now: Date = new Date()): string {
  const minutes = Math.floor((now.getTime() - new Date(iso).getTime()) / 60_000);
  if (minutes < 1) return "Vừa xong";
  if (minutes < 60) return `${minutes} phút trước`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours} giờ trước`;
  const days = Math.floor(hours / 24);
  if (days === 1) return "Hôm qua";
  if (days < 7) return `${days} ngày trước`;
  if (days < 30) return `${Math.floor(days / 7)} tuần trước`;
  if (days < 365) return `${Math.floor(days / 30)} tháng trước`;
  return formatDateVi(iso);
}

export function initials(name: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean);
  if (words.length === 0) return "?";
  if (words.length === 1) return words[0][0].toUpperCase();
  return (words[0][0] + words[words.length - 1][0]).toUpperCase();
}
