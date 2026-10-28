export function searchSummary(q: string, total: number, sort: string): { title: string; subtitle: string } {
  const title = q ? `Kết quả cho “${q}”` : "Tất cả công thức";
  if (total === 0) return { title, subtitle: "Không có kết quả. Thử từ khóa ngắn hơn." };

  const count = `${total.toLocaleString("vi-VN")} công thức`;
  const byRelevance = q && sort.replace(/^-/, "") === "relevance";
  return { title, subtitle: byRelevance ? `${count}, sắp xếp theo độ liên quan.` : `${count}.` };
}
