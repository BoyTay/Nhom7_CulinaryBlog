"use client";

import type { ReactNode } from "react";
import { usePathname } from "next/navigation";
import { showsSiteChrome } from "@/lib/navigation/routes";
import { SiteFooter } from "./site-footer";
import { SiteHeader } from "./site-header";

export function AppChrome({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  if (!showsSiteChrome(pathname)) return <>{children}</>;

  return (
    <>
      <a className="skip-link" href="#main-content">Bỏ qua tới nội dung</a>
      <SiteHeader />
      <div id="main-content" tabIndex={-1}>{children}</div>
      <SiteFooter />
    </>
  );
}
