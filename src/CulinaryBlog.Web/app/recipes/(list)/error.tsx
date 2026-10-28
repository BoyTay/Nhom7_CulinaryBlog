"use client";

import { Alert } from "@/components/shared";

export default function RecipesError({ reset }: { error: Error; reset: () => void }) {
  return (
    <div className="recipes-page ds-scope">
      <main className="recipe-list-main">
        <header className="recipe-list-heading">
          <h1>Tất cả công thức</h1>
        </header>
        <Alert
          variant="error"
          title="Chưa thể tải công thức"
          action={<button className="btn btn-primary btn-square" type="button" onClick={reset}>Thử lại</button>}
        >
          Máy chủ đang gặp sự cố. Vui lòng thử lại sau.
        </Alert>
      </main>
    </div>
  );
}
