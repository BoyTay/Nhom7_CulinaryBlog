"use client";

import { useCallback, useEffect, useRef, useState, type FormEvent } from "react";
import Image from "next/image";
import { ApiError, apiFetch } from "@/lib/api/client";
import { authStore } from "@/lib/auth/auth-store";
import type { CategoryDto } from "@/lib/api/contracts";
import { ConfirmDialog, ConfirmDialogIcon } from "@/components/dashboard/confirm-dialog";
import { Alert, FormField, SkeletonList, useToast } from "@/components/shared";
import { slugifyRecipeTitle } from "@/lib/recipes/editor";
import "./categories.css";

type FormValues = { name: string; description: string; imageUrl: string; orderIndex: string };
type FieldName = keyof FormValues;
type FieldErrors = Partial<Record<FieldName, string>>;

const sessionExpired = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
const currentToken = () => authStore.getAccessToken() ?? "";
const deleteIcon = "M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3";

const emptyForm: FormValues = { name: "", description: "", imageUrl: "", orderIndex: "0" };
const apiFields: Record<string, FieldName> = {
  name: "name", description: "description", imageurl: "imageUrl", orderindex: "orderIndex",
};

function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 401 || error.status === 403) return "Phiên quản trị không hợp lệ hoặc không có quyền Admin.";
    if (error.problem.type === "CATEGORY_NAME_EXISTS") return "Tên danh mục đã tồn tại. Vui lòng chọn tên khác.";
    if (error.problem.type === "CATEGORY_DELETE_HAS_RECIPES") return "Danh mục vẫn còn công thức nên chưa thể xóa.";
    if (error.status === 422) return "Dữ liệu chưa hợp lệ. Vui lòng kiểm tra các trường được đánh dấu.";
    return error.message;
  }
  return "Không thể kết nối API. Vui lòng thử lại.";
}

function imageErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 401 || error.status === 403) return "Phiên quản trị không hợp lệ hoặc không có quyền Admin.";
    if (error.status === 400) return "Tệp không hợp lệ. Chỉ nhận ảnh JPEG, PNG, WebP hoặc AVIF tối đa 5 MB.";
    if (error.status === 404) return "Không tìm thấy danh mục.";
    return error.message;
  }
  return "Không thể kết nối API. Vui lòng thử lại.";
}

function validate(values: FormValues): FieldErrors {
  const errors: FieldErrors = {};
  const name = values.name.trim();
  const slug = name.toLowerCase().replace(/đ/g, "d").normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/[^a-z0-9]/g, "");
  if (name.length < 2 || name.length > 50 || /[<>]/.test(name) || slug.length === 0) {
    errors.name = "Tên cần 2–50 ký tự, không chứa HTML và phải tạo được slug.";
  }
  if (values.description.length > 2000) errors.description = "Mô tả tối đa 2.000 ký tự.";
  if (values.imageUrl.length > 500) errors.imageUrl = "URL hình ảnh tối đa 500 ký tự.";
  if (!Number.isSafeInteger(Number(values.orderIndex)) || Number(values.orderIndex) < 0 || values.orderIndex.trim() === "") {
    errors.orderIndex = "Thứ tự phải là số nguyên không âm.";
  }
  return errors;
}

function serverFieldErrors(error: unknown): FieldErrors {
  if (!(error instanceof ApiError)) return {};
  const fields: FieldErrors = {};
  if (error.problem.type === "CATEGORY_NAME_EXISTS") {
    fields.name = "Tên danh mục đã tồn tại.";
  }
  for (const [key, messages] of Object.entries(error.problem.errors ?? {})) {
    const field = apiFields[key.toLowerCase()];
    if (field) fields[field] = messages.join(" ");
  }
  return fields;
}

export default function CategoryManager() {
  const toast = useToast();
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [listError, setListError] = useState("");
  const [formError, setFormError] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [uploading, setUploading] = useState(false);
  const [editing, setEditing] = useState<CategoryDto | null>(null);
  const [deleting, setDeleting] = useState<CategoryDto | null>(null);
  const [form, setForm] = useState<FormValues>(emptyForm);
  const summaryRef = useRef<HTMLDivElement>(null);
  const listHeadingRef = useRef<HTMLHeadingElement>(null);
  const deleteTriggerRef = useRef<HTMLButtonElement | null>(null);
  const editTriggerRef = useRef<HTMLButtonElement | null>(null);
  const nameInputRef = useRef<HTMLInputElement>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    try {
      setCategories(await apiFetch<CategoryDto[]>("/categories"));
      setListError("");
    } catch (cause) {
      setListError(errorMessage(cause));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { void reload(); }, [reload]);

  function showFormError(message: string, fields: FieldErrors = {}) {
    setFormError(message);
    setFieldErrors(fields);
    requestAnimationFrame(() => summaryRef.current?.focus());
  }

  function beginEdit(category: CategoryDto, trigger: HTMLButtonElement) {
    editTriggerRef.current = trigger;
    setEditing(category);
    setDeleting(null);
    setForm({ name: category.name, description: category.description ?? "", imageUrl: category.imageUrl ?? "", orderIndex: String(category.orderIndex) });
    setFormError("");
    setFieldErrors({});
    // The row turns into a form and the Sửa button is gone, so move keyboard users straight to the first field.
    requestAnimationFrame(() => nameInputRef.current?.focus());
  }

  function cancelEdit() {
    setEditing(null);
    setForm(emptyForm);
    setFormError("");
    setFieldErrors({});
    requestAnimationFrame(() => editTriggerRef.current?.focus());
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError("");
    setFieldErrors({});
    const errors = validate(form);
    if (Object.keys(errors).length > 0) {
      showFormError("Vui lòng sửa các trường chưa hợp lệ.", errors);
      return;
    }
    const token = currentToken();
    if (!token) {
      showFormError(sessionExpired);
      return;
    }
    setBusy(true);
    try {
      const path = editing ? `/categories/${editing.id}` : "/categories";
      await apiFetch<CategoryDto>(path, {
        method: editing ? "PUT" : "POST",
        headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
        body: JSON.stringify({
          name: form.name.trim(), description: form.description.trim() || null,
          imageUrl: form.imageUrl.trim() || null, orderIndex: editing ? Number(form.orderIndex) : categories.length,
        }),
      });
      toast.success(editing ? `Đã lưu danh mục “${form.name.trim()}”` : `Đã thêm danh mục “${form.name.trim()}”`);
      setEditing(null);
      setForm(emptyForm);
      await reload();
      // The submit button was disabled while saving and lost focus, so land on the list heading.
      requestAnimationFrame(() => listHeadingRef.current?.focus());
    } catch (cause) {
      showFormError(errorMessage(cause), serverFieldErrors(cause));
    } finally {
      setBusy(false);
    }
  }

  function beginDelete(category: CategoryDto, trigger: HTMLButtonElement) {
    if (category.recipeCount > 0) return;
    if (!currentToken()) {
      showFormError(sessionExpired);
      return;
    }
    deleteTriggerRef.current = trigger;
    setEditing(null);
    setForm(emptyForm);
    setDeleting(category);
  }

  function cancelDelete() {
    setDeleting(null);
    requestAnimationFrame(() => deleteTriggerRef.current?.focus());
  }

  async function uploadImage(file: File) {
    if (!editing) return;
    const token = currentToken();
    if (!token) {
      showFormError(sessionExpired);
      return;
    }
    setUploading(true);
    setFormError("");
    try {
      const body = new FormData();
      body.append("file", file, file.name);
      const updated = await apiFetch<CategoryDto>(`/categories/${editing.id}/image`, {
        method: "POST",
        headers: { Authorization: `Bearer ${token}` },
        body,
      });
      setEditing(updated);
      setForm((current) => ({ ...current, imageUrl: updated.imageUrl ?? "" }));
      toast.success("Đã tải ảnh danh mục lên.");
      await reload();
    } catch (cause) {
      showFormError(imageErrorMessage(cause));
    } finally {
      setUploading(false);
    }
  }

  async function confirmDelete() {
    if (!deleting) return;
    setBusy(true);
    try {
      await apiFetch<void>(`/categories/${deleting.id}`, {
        method: "DELETE", headers: { Authorization: `Bearer ${currentToken()}` },
      });
      setDeleting(null);
      toast.success(`Đã xóa danh mục “${deleting.name}”`);
      await reload();
      requestAnimationFrame(() => listHeadingRef.current?.focus());
    } catch (cause) {
      setDeleting(null);
      toast.error(errorMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  const newSlug = slugifyRecipeTitle(editing ? "" : form.name) || "ten-danh-muc";

  return (
    <div className="category-admin">
      <div className="category-admin-heading">
        <h1 id="category-list-title" className="rise" ref={listHeadingRef} tabIndex={-1}>Quản lý danh mục</h1>
        <p>Sắp xếp công thức theo nhóm để người đọc dễ duyệt.</p>
      </div>
      {listError && <Alert variant="error">{listError}</Alert>}

      <section className="category-admin-card" aria-labelledby="category-form-title">
        <h2 id="category-form-title">Thêm danh mục</h2>
        {formError && !editing && <Alert variant="error" ref={summaryRef} tabIndex={-1}>{formError}</Alert>}
        <form className="category-admin-add" onSubmit={(event) => void save(event)} noValidate>
          <FormField label="Tên danh mục" error={editing ? undefined : fieldErrors.name} hint={<>Đường dẫn tự sinh: <code>/categories/{newSlug}</code></>} required>
            {(control) => <input {...control} value={editing ? "" : form.name} disabled={Boolean(editing)} onChange={(event) => setForm({ ...form, name: event.target.value })} minLength={2} maxLength={50} required />}
          </FormField>
          <FormField label="Mô tả (không bắt buộc)" error={editing ? undefined : fieldErrors.description}>
            {(control) => <input {...control} value={editing ? "" : form.description} disabled={Boolean(editing)} onChange={(event) => setForm({ ...form, description: event.target.value })} maxLength={2000} placeholder="Sinh tố, trà, cà phê…" />}
          </FormField>
          <button type="submit" className="btn btn-primary category-admin-submit" disabled={busy || Boolean(editing)}>{busy && !editing ? "Đang lưu…" : "Thêm danh mục"}</button>
        </form>
      </section>

      <section className="category-admin-list-section" aria-labelledby="category-table-title">
        <h2 id="category-table-title">Tất cả danh mục <small>· {categories.length}</small></h2>
        {loading ? <SkeletonList label="Đang tải danh mục" /> : !listError && categories.length === 0 ? (
          <p className="category-admin-empty">Chưa có danh mục nào. Hãy tạo danh mục đầu tiên.</p>
        ) : categories.length > 0 && (
          <ul className="category-admin-rows">
            {categories.map((category) => editing?.id === category.id ? (
              <li key={category.id} className="rw category-admin-row is-editing">
                {formError && <Alert variant="error" ref={summaryRef} tabIndex={-1}>{formError}</Alert>}
                <form className="category-admin-edit-form" onSubmit={(event) => void save(event)} noValidate>
                  <FormField label="Tên danh mục" error={fieldErrors.name} hint={<>Đường dẫn <code>{category.slug}</code> không đổi khi đổi tên</>} required>
                    {(control) => <input {...control} ref={nameInputRef} value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} minLength={2} maxLength={50} required />}
                  </FormField>
                  <FormField label="Mô tả" error={fieldErrors.description}>
                    {(control) => <input {...control} value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} maxLength={2000} />}
                  </FormField>
                  <FormField label="Thứ tự hiển thị" error={fieldErrors.orderIndex} required>
                    {(control) => <input {...control} type="number" min={0} step={1} value={form.orderIndex} onChange={(event) => setForm({ ...form, orderIndex: event.target.value })} required />}
                  </FormField>
                  <div className="category-admin-image">
                    {editing.imageUrl && (
                      <Image src={editing.imageUrl} alt={`Ảnh danh mục ${editing.name}`} width={120} height={72} unoptimized className="category-admin-image-preview" />
                    )}
                    <label className="category-admin-file">
                      <span>{uploading ? "Đang tải ảnh…" : editing.imageUrl ? "Đổi ảnh danh mục" : "Tải ảnh danh mục"}</span>
                      <input
                        type="file"
                        accept="image/jpeg,image/png,image/webp,image/avif"
                        disabled={uploading || busy}
                        onChange={(event) => {
                          const file = event.target.files?.[0];
                          event.target.value = "";
                          if (file) void uploadImage(file);
                        }}
                      />
                    </label>
                    <span className="category-admin-help">JPEG, PNG, WebP hoặc AVIF, tối đa 5 MB. Ảnh được lưu ngay khi tải lên.</span>
                  </div>
                  <div className="category-admin-edit-actions">
                    <span className="category-admin-count">{category.recipeCount} công thức</span>
                    <button type="button" className="btn btn-outline" onClick={cancelEdit} disabled={busy}>Hủy</button>
                    <button type="submit" className="btn btn-primary" disabled={busy}>{busy ? "Đang lưu…" : "Lưu"}</button>
                  </div>
                </form>
              </li>
            ) : (
              <li key={category.id} className="rw category-admin-row">
                <div className="category-admin-row-name">
                  <strong>{category.name}</strong>
                  <span><code>{category.slug}</code></span>
                </div>
                <span className="category-admin-count">{category.recipeCount} công thức</span>
                <div className="category-admin-row-actions">
                  {category.recipeCount > 0 && (
                    <span id={`delete-blocked-${category.id}`} className="category-admin-why">
                      Còn {category.recipeCount} công thức nên chưa xóa được
                    </span>
                  )}
                  <button type="button" className="btn btn-outline" onClick={(event) => beginEdit(category, event.currentTarget)} aria-label={`Sửa danh mục ${category.name}`}>Sửa</button>
                  {/* aria-disabled (not disabled) keeps the button focusable so the reason is reachable by keyboard and screen reader. */}
                  <button
                    type="button"
                    className="btn btn-outline btn-icon category-admin-delete"
                    disabled={busy}
                    aria-disabled={category.recipeCount > 0 ? true : undefined}
                    aria-describedby={category.recipeCount > 0 ? `delete-blocked-${category.id}` : undefined}
                    onClick={(event) => beginDelete(category, event.currentTarget)}
                    aria-label={`Xóa danh mục ${category.name}`}
                  >
                    <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.9" strokeLinecap="round" strokeLinejoin="round"><path d={deleteIcon} /></svg>
                  </button>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>

      <ConfirmDialog
        open={deleting !== null}
        title={deleting ? `Xóa danh mục “${deleting.name}”?` : ""}
        description="Danh mục chưa có công thức nào. Bạn không thể hoàn tác thao tác này."
        confirmLabel={busy ? "Đang xóa…" : "Xóa danh mục"}
        destructive
        icon={<ConfirmDialogIcon path={deleteIcon} tone="danger" />}
        onCancel={cancelDelete}
        onConfirm={() => void confirmDelete()}
      />
    </div>
  );
}
