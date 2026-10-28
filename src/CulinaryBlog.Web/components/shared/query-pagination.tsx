"use client";

import { usePathname, useRouter } from "next/navigation";
import { Pagination } from "./pagination";

export function QueryPagination({ page, totalPages, label }: { page: number; totalPages: number; label?: string }) {
  const pathname = usePathname();
  const router = useRouter();

  return (
    <Pagination
      label={label}
      page={page}
      totalPages={totalPages}
      onPageChange={(next) => router.push(next > 1 ? `${pathname}?page=${next}` : pathname)}
    />
  );
}
