"use client";

import { ServerErrorScreen } from "@/components/shared/status-screen";

export default function ErrorPage({ reset }: { error: Error; reset: () => void }) {
  return <ServerErrorScreen onRetry={reset} />;
}
