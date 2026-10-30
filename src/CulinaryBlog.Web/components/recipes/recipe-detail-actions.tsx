"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useToast } from "@/components/shared/toast";

function useShare(title: string) {
  const toast = useToast();

  return async function share() {
    const url = window.location.href;
    if (navigator.share) {
      await navigator.share({ title, url }).catch(() => undefined);
      return;
    }
    await navigator.clipboard.writeText(url);
    toast.success("Đã sao chép liên kết công thức");
  };
}

export function RecipeMobileBar({ title, categoryName, backHref }: { title: string; categoryName: string; backHref: string }) {
  const share = useShare(title);
  return (
    <div className="noprint recipe-mobile-bar">
      <Link href={backHref} className="recipe-mobile-icon" aria-label="Quay lại">
        <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"><path d="M15 5l-7 7 7 7" /></svg>
      </Link>
      <span>{categoryName}</span>
      <button type="button" className="recipe-mobile-icon" aria-label="Chia sẻ" onClick={() => void share()}>
        <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="18" cy="5" r="3" /><circle cx="6" cy="12" r="3" /><circle cx="18" cy="19" r="3" /><path d="M8.6 10.6l6.8-4M8.6 13.4l6.8 4" /></svg>
      </button>
    </div>
  );
}

export function RecipeActions({ title }: { title: string }) {
  const share = useShare(title);

  return (
    <div className="noprint recipe-detail-actions">
      <button type="button" className="btn btn-outline" onClick={() => window.print()}>
        <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M7 9V3h10v6M7 17H4v-8h16v8h-3M7 14h10v7H7z" /></svg>
        In công thức
      </button>
      <button type="button" className="btn btn-outline" onClick={() => void share()}>
        <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M12 15V4M8 8l4-4 4 4M5 13v7h14v-7" /></svg>
        Chia sẻ
      </button>
    </div>
  );
}

function clockLabel(seconds: number): string {
  const minutes = Math.floor(seconds / 60);
  return `${minutes}:${String(seconds % 60).padStart(2, "0")}`;
}

export function StepTimer({ minutes }: { minutes: number }) {
  const [remaining, setRemaining] = useState<number | null>(null);

  useEffect(() => {
    if (remaining === null || remaining <= 0) return;
    const timer = window.setTimeout(() => setRemaining(remaining - 1), 1000);
    return () => window.clearTimeout(timer);
  }, [remaining]);

  const label = remaining === null
    ? `Hẹn giờ ${minutes} phút`
    : remaining === 0
      ? "Đã hết giờ. Bấm để hẹn lại"
      : `Đang đếm ${clockLabel(remaining)}. Bấm để dừng`;

  return (
    <button
      type="button"
      className="btn btn-outline noprint recipe-detail-timer"
      aria-live="off"
      onClick={() => setRemaining(remaining === null || remaining === 0 ? minutes * 60 : null)}
    >
      <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><circle cx="12" cy="13" r="8" /><path d="M12 9v4l2 2M9 2h6" /></svg>
      {label}
    </button>
  );
}
