"use client";

import Image from "next/image";
import { useRouter } from "next/navigation";
import { useRef, useState, type ChangeEvent, type FormEvent } from "react";
import { ApiError } from "@/lib/api/client";
import { describeAuthError } from "@/lib/auth/auth-errors";
import { useAuth } from "@/lib/auth/use-auth";

const maxAvatarBytes = 5 * 1024 * 1024;
const avatarTypes = ["image/jpeg", "image/png", "image/webp", "image/avif"];

function getInitials(name?: string, email?: string): string {
  if (name && name.trim()) {
    const parts = name.trim().split(/\s+/);
    if (parts.length >= 2) {
      return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    }
    return parts[0].slice(0, 2).toUpperCase();
  }
  if (email && email.trim()) {
    return email.trim().slice(0, 2).toUpperCase();
  }
  return "CB";
}

function getRoleLabel(roles: string[] = []): { label: string; isAdmin: boolean } {
  const lowerRoles = roles.map((r) => r.toLowerCase());
  if (lowerRoles.includes("admin") || lowerRoles.includes("administrator")) {
    return { label: "Quản trị viên", isAdmin: true };
  }
  if (lowerRoles.includes("chef") || lowerRoles.includes("author") || lowerRoles.includes("creator")) {
    return { label: "Tác giả", isAdmin: false };
  }
  return { label: "Độc giả", isAdmin: false };
}

export function ProfileForm() {
  const router = useRouter();
  const { user, updateProfile, uploadAvatar, logout } = useAuth();
  const avatarInputRef = useRef<HTMLInputElement>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [uploadMessage, setUploadMessage] = useState<string | null>(null);
  const [uploadError, setUploadError] = useState<string | null>(null);

  const [displayName, setDisplayName] = useState(user?.displayName ?? "");
  const [avatarUrl, setAvatarUrl] = useState(user?.avatarUrl ?? "");
  const [bio, setBio] = useState(user?.bio ?? "");

  const [isSaving, setIsSaving] = useState(false);
  const [isLoggingOut, setIsLoggingOut] = useState(false);
  const [successSaved, setSuccessSaved] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<{ displayName?: string; avatarUrl?: string }>({});
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const initials = getInitials(user?.displayName, user?.email);
  const roleInfo = getRoleLabel(user?.roles);

  const validate = (): boolean => {
    const errors: { displayName?: string; avatarUrl?: string } = {};
    const trimmed = displayName.trim();

    if (!trimmed) {
      errors.displayName = "Vui lòng nhập tên hiển thị.";
    } else if (trimmed.length < 2 || trimmed.length > 100) {
      errors.displayName = "Tên hiển thị phải từ 2 đến 100 ký tự.";
    } else if (/[<>]/.test(trimmed)) {
      errors.displayName = "Tên hiển thị không được chứa thẻ HTML.";
    }

    if (avatarUrl && avatarUrl.trim()) {
      try {
        const parsed = new URL(avatarUrl.trim());
        if (!["http:", "https:"].includes(parsed.protocol)) {
          errors.avatarUrl = "Đường dẫn ảnh đại diện phải bắt đầu bằng http:// hoặc https://";
        }
      } catch {
        errors.avatarUrl = "Địa chỉ URL ảnh đại diện không hợp lệ.";
      }
    }

    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setSuccessSaved(false);
    setErrorMessage(null);

    if (!validate()) {
      return;
    }

    setIsSaving(true);
    try {
      await updateProfile({
        displayName: displayName.trim(),
        avatarUrl: avatarUrl.trim(),
        bio: bio.trim(),
      });
      setSuccessSaved(true);
    } catch (err) {
      if (err instanceof ApiError) {
        if ((err.status === 400 || err.status === 422) && err.problem.errors) {
          const serverErrors: typeof fieldErrors = {};
          for (const [key, messages] of Object.entries(err.problem.errors)) {
            const k = key.toLowerCase();
            if (k.includes("name") || k.includes("display")) {
              serverErrors.displayName = messages.join(" ");
            } else if (k.includes("avatar")) {
              serverErrors.avatarUrl = messages.join(" ");
            }
          }
          setFieldErrors(serverErrors);
          setErrorMessage("Vui lòng kiểm tra lại các trường thông tin được đánh dấu.");
        } else {
          setErrorMessage(err.problem.detail || err.message);
        }
      } else {
        setErrorMessage("Không thể kết nối đến máy chủ. Vui lòng kiểm tra lại kết nối mạng.");
      }
    } finally {
      setIsSaving(false);
    }
  };

  const handleAvatarSelected = async (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = "";
    setUploadMessage(null);
    setUploadError(null);
    if (!file) return;

    if (!avatarTypes.includes(file.type)) {
      setUploadError("Chỉ chấp nhận ảnh JPEG, PNG, WebP hoặc AVIF.");
      return;
    }
    if (file.size > maxAvatarBytes) {
      setUploadError("Ảnh vượt quá giới hạn 5 MB.");
      return;
    }

    setIsUploading(true);
    try {
      const updated = await uploadAvatar(file);
      setAvatarUrl(updated.avatarUrl ?? "");
      setUploadMessage("Đã cập nhật ảnh đại diện.");
    } catch (err) {
      setUploadError(describeAuthError(err, "Không thể tải ảnh lên. Vui lòng thử lại."));
    } finally {
      setIsUploading(false);
    }
  };

  const handleLogout = async () => {
    setIsLoggingOut(true);
    try {
      await logout();
      router.push("/");
    } catch {
      router.push("/");
    } finally {
      setIsLoggingOut(false);
    }
  };

  return (
    <section className="profile-card">
      {/* Identity Block (Avatar 88px + Name + Role Badge) */}
      <div className="profile-identity">
        <div className="profile-avatar-88" aria-hidden="true">
          {user?.avatarUrl ? (
            <Image
              src={user.avatarUrl}
              alt={user.displayName || "Avatar"}
              width={88}
              height={88}
              unoptimized
            />
          ) : (
            <span>{initials}</span>
          )}
        </div>
        <div className="profile-user-info">
          <strong className="profile-user-name">{user?.displayName || "Người dùng"}</strong>
          <span
            className={`profile-role-badge ${roleInfo.isAdmin ? "profile-role-badge-admin" : ""}`}
          >
            {roleInfo.label}
          </span>
        </div>
      </div>

      <div className="profile-field-group">
        <div className="profile-avatar-upload">
          <input
            ref={avatarInputRef}
            id="avatar-file"
            type="file"
            accept={avatarTypes.join(",")}
            className="profile-visually-hidden"
            tabIndex={-1}
            aria-hidden="true"
            onChange={handleAvatarSelected}
          />
          <button
            type="button"
            className="profile-btn profile-btn-secondary"
            disabled={isUploading || isSaving}
            onClick={() => avatarInputRef.current?.click()}
          >
            {isUploading ? "Đang tải ảnh lên..." : "Tải ảnh đại diện từ máy"}
          </button>
          <span className="profile-hint">JPEG, PNG, WebP hoặc AVIF, tối đa 5 MB. Ảnh được lưu trên MinIO.</span>
        </div>
        {uploadMessage && (
          <div role="status" className="profile-alert-success">
            <span>{uploadMessage}</span>
          </div>
        )}
        {uploadError && (
          <div role="alert" className="profile-alert-error">
            <span>{uploadError}</span>
          </div>
        )}
      </div>

      <form className="profile-form" onSubmit={handleSubmit} noValidate>
        {/* Display Name */}
        <div className="profile-field-group">
          <label className="profile-label" htmlFor="fn">
            Tên hiển thị
          </label>
          <input
            id="fn"
            name="displayName"
            type="text"
            className="profile-input"
            value={displayName}
            disabled={isSaving}
            aria-invalid={Boolean(fieldErrors.displayName)}
            aria-describedby="fn-hint"
            placeholder="Tên hiển thị của bạn"
            onChange={(e) => {
              setDisplayName(e.target.value);
              setSuccessSaved(false);
              if (fieldErrors.displayName) {
                setFieldErrors((prev) => ({ ...prev, displayName: undefined }));
              }
            }}
          />
          <span id="fn-hint" className="profile-hint">
            2–100 ký tự.
          </span>
          {fieldErrors.displayName && (
            <span className="profile-field-error" role="alert">
              {fieldErrors.displayName}
            </span>
          )}
        </div>

        {/* Avatar URL */}
        <div className="profile-field-group">
          <label className="profile-label" htmlFor="av">
            Hoặc dán URL ảnh đại diện
          </label>
          <input
            id="av"
            name="avatarUrl"
            type="url"
            className="profile-input"
            value={avatarUrl}
            disabled={isSaving}
            aria-invalid={Boolean(fieldErrors.avatarUrl)}
            aria-describedby={fieldErrors.avatarUrl ? "av-error" : undefined}
            placeholder="https://…"
            onChange={(e) => {
              setAvatarUrl(e.target.value);
              setSuccessSaved(false);
              if (fieldErrors.avatarUrl) {
                setFieldErrors((prev) => ({ ...prev, avatarUrl: undefined }));
              }
            }}
          />
          {fieldErrors.avatarUrl && (
            <span id="av-error" className="profile-field-error" role="alert">
              {fieldErrors.avatarUrl}
            </span>
          )}
        </div>

        {/* Bio */}
        <div className="profile-field-group">
          <label className="profile-label" htmlFor="bio">
            Giới thiệu ngắn
          </label>
          <textarea
            id="bio"
            name="bio"
            className="profile-input profile-textarea"
            value={bio}
            disabled={isSaving}
            rows={3}
            placeholder="Chia sẻ đôi nét về niềm đam mê ẩm thực của bạn..."
            onChange={(e) => {
              setBio(e.target.value);
              setSuccessSaved(false);
            }}
          />
        </div>

        {/* Read-only Email */}
        <div className="profile-field-group">
          <label className="profile-label" htmlFor="em">
            Email
          </label>
          <input
            id="em"
            type="email"
            className="profile-input"
            value={user?.email || ""}
            readOnly
            aria-describedby="emh"
          />
          <span id="emh" className="profile-hint">
            Email không thể đổi tại đây.
          </span>
        </div>

        {/* Read-only User Id */}
        <div className="profile-field-group">
          <label className="profile-label" htmlFor="uid">
            Mã định danh tài khoản
          </label>
          <input
            id="uid"
            type="text"
            className="profile-input"
            value={user?.id || ""}
            readOnly
          />
        </div>

        {/* Status / Error Alerts */}
        {successSaved && (
          <div role="status" className="profile-alert-success">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
              <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" />
              <polyline points="22 4 12 14.01 9 11.01" />
            </svg>
            <span>Đã lưu thông tin hồ sơ thành công!</span>
          </div>
        )}

        {errorMessage && (
          <div role="alert" className="profile-alert-error">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
              <circle cx="12" cy="12" r="10" />
              <line x1="15" y1="9" x2="9" y2="15" />
              <line x1="9" y1="9" x2="15" y2="15" />
            </svg>
            <span>{errorMessage}</span>
          </div>
        )}

        {/* Action Buttons */}
        <div className="profile-actions">
          <button
            type="submit"
            className="profile-btn profile-btn-primary"
            disabled={isSaving || isLoggingOut}
          >
            {isSaving ? "Đang lưu..." : "Lưu thay đổi"}
          </button>
          <button
            type="button"
            className="profile-btn profile-btn-logout"
            onClick={handleLogout}
            disabled={isSaving || isLoggingOut}
          >
            {isLoggingOut ? "Đang đăng xuất..." : "Đăng xuất"}
          </button>
        </div>
      </form>
    </section>
  );
}

