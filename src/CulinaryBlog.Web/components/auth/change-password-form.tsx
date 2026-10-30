"use client";

import { useState, type FormEvent } from "react";
import { describeAuthError, fieldErrorsOf, passwordPolicyError } from "@/lib/auth/auth-errors";
import { useAuth } from "@/lib/auth/use-auth";

interface ChangePasswordErrors {
  currentPassword?: string;
  newPassword?: string;
  confirmPassword?: string;
}

export function ChangePasswordForm() {
  const { changePassword } = useAuth();

  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<ChangePasswordErrors>({});
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const validate = (): boolean => {
    const errors: ChangePasswordErrors = {};
    if (!currentPassword) errors.currentPassword = "Vui lòng nhập mật khẩu hiện tại.";
    const policyError = passwordPolicyError(newPassword);
    if (policyError) {
      errors.newPassword = policyError;
    } else if (newPassword === currentPassword) {
      errors.newPassword = "Mật khẩu mới phải khác mật khẩu hiện tại.";
    }
    if (newPassword !== confirmPassword) errors.confirmPassword = "Mật khẩu xác nhận không khớp.";
    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setErrorMessage(null);
    if (!validate()) return;

    setIsSubmitting(true);
    try {
      await changePassword({ currentPassword, newPassword });
      // The server revoked every session, including this one; a full load resets all in-memory auth state.
      window.location.replace("/login?notice=password-changed");
    } catch (err) {
      const serverErrors = fieldErrorsOf(err);
      if (serverErrors.currentpassword || serverErrors.newpassword) {
        setFieldErrors({
          currentPassword: serverErrors.currentpassword,
          newPassword: serverErrors.newpassword,
        });
      } else {
        setErrorMessage(describeAuthError(err, "Không thể đổi mật khẩu. Vui lòng thử lại."));
      }
      setIsSubmitting(false);
    }
  };

  const fields: Array<{
    id: string;
    key: keyof ChangePasswordErrors;
    label: string;
    value: string;
    setValue: (value: string) => void;
    autoComplete: string;
  }> = [
    { id: "cp-current", key: "currentPassword", label: "Mật khẩu hiện tại", value: currentPassword, setValue: setCurrentPassword, autoComplete: "current-password" },
    { id: "cp-new", key: "newPassword", label: "Mật khẩu mới", value: newPassword, setValue: setNewPassword, autoComplete: "new-password" },
    { id: "cp-confirm", key: "confirmPassword", label: "Xác nhận mật khẩu mới", value: confirmPassword, setValue: setConfirmPassword, autoComplete: "new-password" },
  ];

  return (
    <section className="rise profile-card" aria-labelledby="cp-title">
      <div className="profile-section-head">
        <h2 id="cp-title" className="profile-section-title">Đổi mật khẩu</h2>
        <p className="profile-hint">
          Mật khẩu mới cần ít nhất 8 ký tự gồm chữ hoa, chữ thường, số và ký tự đặc biệt.
          Sau khi đổi, mọi thiết bị sẽ bị đăng xuất và bạn cần đăng nhập lại.
        </p>
      </div>

      <form className="profile-form" onSubmit={handleSubmit} noValidate>
        {fields.map((field) => (
          <div className="profile-field-group" key={field.id}>
            <label className="profile-label" htmlFor={field.id}>
              {field.label}
            </label>
            <input
              id={field.id}
              type={showPassword ? "text" : "password"}
              autoComplete={field.autoComplete}
              className="profile-input"
              value={field.value}
              disabled={isSubmitting}
              aria-invalid={Boolean(fieldErrors[field.key])}
              aria-describedby={fieldErrors[field.key] ? `${field.id}-error` : undefined}
              onChange={(e) => {
                field.setValue(e.target.value);
                if (fieldErrors[field.key]) {
                  setFieldErrors((prev) => ({ ...prev, [field.key]: undefined }));
                }
              }}
            />
            {fieldErrors[field.key] && (
              <span id={`${field.id}-error`} className="profile-field-error" role="alert">
                {fieldErrors[field.key]}
              </span>
            )}
          </div>
        ))}

        <label className="profile-checkbox">
          <input
            type="checkbox"
            checked={showPassword}
            onChange={(e) => setShowPassword(e.target.checked)}
          />
          <span>Hiển thị mật khẩu</span>
        </label>

        {errorMessage && (
          <div role="alert" className="profile-alert-error">
            <span>{errorMessage}</span>
          </div>
        )}

        <div>
          <button type="submit" className="profile-btn profile-btn-primary" disabled={isSubmitting}>
            {isSubmitting ? "Đang cập nhật..." : "Đổi mật khẩu"}
          </button>
        </div>
      </form>
    </section>
  );
}
