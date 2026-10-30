"use client";

import { ServerErrorScreen } from "@/components/shared/status-screen";

export default function RecipeError({ reset }: { error: Error; reset: () => void }) {
  return <ServerErrorScreen onRetry={reset} />;
}
