import type { ComponentPropsWithRef, ReactNode } from "react";

export type AlertVariant = "error" | "success" | "warning" | "info";

type AlertProps = Omit<ComponentPropsWithRef<"div">, "title"> & {
  variant?: AlertVariant;
  title?: ReactNode;
  action?: ReactNode;
};

/** Errors are announced assertively (role="alert"); other variants politely (role="status"). */
export function Alert({ variant = "info", title, action, className, children, ...rest }: AlertProps) {
  const classes = ["ui-alert", `ui-alert-${variant}`, className].filter(Boolean).join(" ");
  return (
    <div className={classes} role={variant === "error" ? "alert" : "status"} {...rest}>
      <div className="ui-alert-body">
        {title && <strong className="ui-alert-title">{title}</strong>}
        <div>{children}</div>
      </div>
      {action && <div className="ui-alert-action">{action}</div>}
    </div>
  );
}
