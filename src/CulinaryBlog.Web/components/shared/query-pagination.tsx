"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { Pagination } from "./pagination";

/** Pagination that writes ?page= while keeping every other query parameter (filters, search text, sort). */
export function QueryPagination({ page, totalPages, label }: { page: number; totalPages: number; label?: string }) {
  const pathname = usePathname();
  const router = useRouter();
  const searchParams = useSearchParams();

  function go(next: number) {
    const params = new URLSearchParams(searchParams.toString());
    if (next > 1) params.set("page", String(next));
    else params.delete("page");
    const query = params.toString();
    router.push(query ? `${pathname}?${query}` : pathname);
  }

  return <Pagination label={label} page={page} totalPages={totalPages} onPageChange={go} />;
}
