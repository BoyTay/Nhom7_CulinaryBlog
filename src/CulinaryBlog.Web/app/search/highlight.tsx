import type { ReactNode } from "react";

/** Lowercases and strips Vietnamese diacritics one character at a time so indexes stay aligned with the source. */
function foldChar(char: string): string {
  if (char === "đ" || char === "Đ") return "d";
  return char.normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase();
}

/** Wraps parts of `text` matching any query word in <mark>, ignoring case and diacritics ("pho bo" matches "Phở bò"). */
export function highlightMatches(text: string, query: string): ReactNode {
  const words = query.split(/\s+/).map((word) => Array.from(word, foldChar).join("")).filter((word) => word.length >= 2);
  if (words.length === 0) return text;

  const chars = Array.from(text);
  const folded = chars.map((char) => foldChar(char) || char).join("");
  // Only highlight when folding kept one folded character per source character.
  if (folded.length !== chars.length) return text;

  const marked = new Array<boolean>(chars.length).fill(false);
  for (const word of words) {
    for (let index = folded.indexOf(word); index !== -1; index = folded.indexOf(word, index + 1)) {
      for (let offset = 0; offset < word.length; offset++) marked[index + offset] = true;
    }
  }

  const parts: ReactNode[] = [];
  let start = 0;
  while (start < chars.length) {
    let end = start;
    while (end < chars.length && marked[end] === marked[start]) end++;
    const slice = chars.slice(start, end).join("");
    parts.push(marked[start] ? <mark key={start}>{slice}</mark> : slice);
    start = end;
  }
  return parts;
}
