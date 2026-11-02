"use client";

import { createContext, useContext, useState, useSyncExternalStore, type ReactNode } from "react";
import { createToastStore, type ToastStore } from "@/lib/ui/toast-store";

const ToastContext = createContext<ToastStore | null>(null);

export function ToastProvider({ children }: { children: ReactNode }) {
  const [store] = useState(() => createToastStore({ durationMs: 5000, maxVisible: 3 }));
  const toasts = useSyncExternalStore(store.subscribe, store.getSnapshot, store.getSnapshot);

  return (
    <ToastContext.Provider value={store}>
      {children}
      <div className="toast-region">
        {toasts.map((toast) => (
          <div key={toast.id} className={`toast toast-${toast.kind}`} role={toast.role}>
            <span className="toast-mark" aria-hidden="true">
              {toast.kind === "success" ? (
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round"><path d="M5 12.5l4.5 4.5L19 7.5" /></svg>
              ) : (
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16.5h.01" /></svg>
              )}
            </span>
            <span className="toast-text">{toast.message}</span>
            <button type="button" aria-label="Đóng thông báo" onClick={() => store.dismiss(toast.id)}>×</button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast(): { success(message: string): void; error(message: string): void } {
  const store = useContext(ToastContext);
  if (!store) throw new Error("useToast must be used inside ToastProvider");
  return {
    success: (message) => void store.push("success", message),
    error: (message) => void store.push("error", message),
  };
}
