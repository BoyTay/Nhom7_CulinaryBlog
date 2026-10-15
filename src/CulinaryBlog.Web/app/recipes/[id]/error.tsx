"use client";

import Link from "next/link";
import { Alert, SiteHeader } from "@/components/shared";

export default function RecipeDetailError({ reset }: { error: Error; reset: () => void }) {
  return (
    <div className="recipe-detail-page ds-scope">
      <SiteHeader />
      <main className="recipe-detail-state">
        <Alert
          variant="error"
          title="Chưa thể tải công thức"
          action={(
            <div className="recipe-detail-state-actions">
              <button type="button" onClick={reset}>Thử lại</button>
              <Link href="/recipes">Về danh sách</Link>
            </div>
          )}
        >
          Máy chủ đang gặp sự cố. Vui lòng thử lại sau.
        </Alert>
      </main>
    </div>
  );
}
