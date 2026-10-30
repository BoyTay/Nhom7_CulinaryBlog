"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { parseStatusTab, type StatusTab } from "@/lib/dashboard/my-recipes";

const tabs: { value: StatusTab; label: string }[] = [
  { value: "all", label: "Tất cả" },
  { value: "draft", label: "Bản nháp" },
  { value: "published", label: "Đã xuất bản" },
  { value: "archived", label: "Lưu trữ" },
];

export function useStatusTab(): StatusTab {
  return parseStatusTab(useSearchParams().get("status") ?? undefined);
}

export function StatusTabs() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const current = useStatusTab();

  function select(tab: StatusTab) {
    const params = new URLSearchParams(searchParams.toString());
    if (tab === "all") params.delete("status");
    else params.set("status", tab);
    const query = params.toString();
    router.replace(query ? `${pathname}?${query}` : pathname);
  }

  return (
    <div role="tablist" aria-label="Trạng thái" className="dashboard-tabs">
      {tabs.map((tab) => (
        <button
          key={tab.value}
          type="button"
          role="tab"
          aria-selected={current === tab.value}
          className="chip"
          onClick={() => select(tab.value)}
        >
          {tab.label}
        </button>
      ))}
    </div>
  );
}
