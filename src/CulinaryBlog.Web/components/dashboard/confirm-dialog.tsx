"use client";

import { useEffect, useRef, type ReactNode } from "react";

export interface ConfirmDialogProps {
  open: boolean;
  title: string;
  description: string;
  confirmLabel: string;
  destructive?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
  /** Optional mark above the title and extra controls below the description. */
  icon?: ReactNode;
  children?: ReactNode;
  className?: string;
}

export function ConfirmDialogIcon({ path, tone }: { path: string; tone: "danger" | "rose" | "gold" | "leaf" }) {
  return (
    <span className={`confirm-dialog-icon is-${tone}`}>
      <svg aria-hidden="true" width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d={path} /></svg>
    </span>
  );
}

export function ConfirmDialog({ open, title, description, confirmLabel, destructive, onConfirm, onCancel, icon, children, className }: ConfirmDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const returnFocusRef = useRef<HTMLElement | null>(null);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;
    if (open && !dialog.open) {
      returnFocusRef.current = document.activeElement as HTMLElement | null;
      dialog.showModal();
    } else if (!open && dialog.open) {
      dialog.close();
      returnFocusRef.current?.focus();
    }
  }, [open]);

  return (
    <dialog ref={dialogRef} className={["confirm-dialog", className].filter(Boolean).join(" ")} aria-labelledby="confirm-title" aria-describedby="confirm-desc" onCancel={(event) => { event.preventDefault(); onCancel(); }}>
      {icon}
      <h2 id="confirm-title">{title}</h2>
      <p id="confirm-desc">{description}</p>
      {children}
      <div className="confirm-dialog-actions">
        <button type="button" className="btn btn-outline" onClick={onCancel}>Giữ nguyên</button>
        <button type="button" className={destructive ? "btn btn-danger" : "btn btn-primary"} onClick={onConfirm}>{confirmLabel}</button>
      </div>
    </dialog>
  );
}
