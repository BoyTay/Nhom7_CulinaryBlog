export type ToastKind = "success" | "error";

export interface Toast {
  id: number;
  kind: ToastKind;
  message: string;
  role: "status" | "alert";
}

export interface ToastStore {
  push(kind: ToastKind, message: string): number;
  dismiss(id: number): void;
  getSnapshot(): readonly Toast[];
  subscribe(listener: () => void): () => void;
}

export function createToastStore(options: { durationMs: number; maxVisible: number }): ToastStore {
  let toasts: readonly Toast[] = [];
  let nextId = 1;
  const listeners = new Set<() => void>();

  const publish = (next: readonly Toast[]) => {
    toasts = next;
    listeners.forEach((listener) => listener());
  };

  const dismiss = (id: number) => publish(toasts.filter((toast) => toast.id !== id));

  return {
    push(kind, message) {
      const id = nextId++;
      const toast: Toast = { id, kind, message, role: kind === "error" ? "alert" : "status" };
      publish([...toasts, toast].slice(-options.maxVisible));
      const timer = setTimeout(() => dismiss(id), options.durationMs);
      (timer as { unref?: () => void }).unref?.();
      return id;
    },
    dismiss,
    getSnapshot: () => toasts,
    subscribe(listener) {
      listeners.add(listener);
      return () => listeners.delete(listener);
    },
  };
}
