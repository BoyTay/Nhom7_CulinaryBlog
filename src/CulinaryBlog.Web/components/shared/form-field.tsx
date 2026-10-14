import { useId, type ReactNode } from "react";

export type FieldControlProps = {
  id: string;
  "aria-invalid": true | undefined;
  "aria-describedby": string | undefined;
};

type FormFieldProps = {
  label: ReactNode;
  error?: string;
  hint?: ReactNode;
  required?: boolean;
  className?: string;
  /** Render the control (input, textarea, select) and spread `controlProps` onto it. */
  children: (controlProps: FieldControlProps) => ReactNode;
};

/** Label + control + hint + inline error, wired with htmlFor, aria-invalid and aria-describedby. */
export function FormField({ label, error, hint, required, className, children }: FormFieldProps) {
  const id = useId();
  const hintId = hint ? `${id}-hint` : undefined;
  const errorId = error ? `${id}-error` : undefined;
  const describedBy = [hintId, errorId].filter(Boolean).join(" ") || undefined;

  return (
    <div className={["ui-field", className].filter(Boolean).join(" ")}>
      <label htmlFor={id}>
        {label}
        {required && <span className="ui-field-required" aria-hidden="true"> *</span>}
      </label>
      {children({ id, "aria-invalid": error ? true : undefined, "aria-describedby": describedBy })}
      {hint && <span id={hintId} className="ui-field-hint">{hint}</span>}
      {error && <span id={errorId} className="ui-field-error">{error}</span>}
    </div>
  );
}
