"use client";

import { useEffect, useRef } from "react";

export interface ConfirmDialogProps {
  open: boolean;
  title: string;
  description: string;
  confirmLabel: string;
  destructive?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export function ConfirmDialog({ open, title, description, confirmLabel, destructive, onConfirm, onCancel }: ConfirmDialogProps) {
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
    <dialog ref={dialogRef} className="confirm-dialog" aria-labelledby="confirm-title" aria-describedby="confirm-desc" onCancel={(event) => { event.preventDefault(); onCancel(); }}>
      <h2 id="confirm-title">{title}</h2>
      <p id="confirm-desc">{description}</p>
      <div className="confirm-dialog-actions">
        <button type="button" className="btn btn-outline" onClick={onCancel}>Giữ nguyên</button>
        <button type="button" className={destructive ? "btn btn-danger" : "btn btn-primary"} onClick={onConfirm}>{confirmLabel}</button>
      </div>
    </dialog>
  );
}
