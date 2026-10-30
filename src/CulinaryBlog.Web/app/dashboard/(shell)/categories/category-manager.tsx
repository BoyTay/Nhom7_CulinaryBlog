"use client";

import { useCallback, useEffect, useRef, useState, type FormEvent } from "react";
import Image from "next/image";
import { ApiError, apiFetch } from "@/lib/api/client";
import { authStore } from "@/lib/auth/auth-store";
import type { CategoryDto } from "@/lib/api/contracts";
import { Alert, FormField, SkeletonList } from "@/components/shared";
import "./categories.css";

type FormValues = { name: string; description: string; imageUrl: string; orderIndex: string };
type FieldName = keyof FormValues;
type FieldErrors = Partial<Record<FieldName, string>>;

const sessionExpired = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
const currentToken = () => authStore.getAccessToken() ?? "";

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
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [listError, setListError] = useState("");
  const [formError, setFormError] = useState("");
  const [deleteError, setDeleteError] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [notice, setNotice] = useState("");
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
    setNotice("");
    // The form sits after the whole table in tab order, so move keyboard users straight to it.
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
    setNotice("");
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
      setNotice(editing ? "Đã cập nhật danh mục." : "Đã tạo danh mục.");
      setEditing(null);
      setForm(emptyForm);
      await reload();
      // The submit button was disabled while saving and lost focus; land on the list heading next to the notice.
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
    setDeleteError("");
    setNotice("");
    setEditing(null);
    setForm(emptyForm);
    setDeleting(category);
  }

  function cancelDelete() {
    setDeleting(null);
    setDeleteError("");
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
    setNotice("");
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
      setNotice("Đã tải ảnh danh mục lên.");
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
    setDeleteError("");
    try {
      await apiFetch<void>(`/categories/${deleting.id}`, {
        method: "DELETE", headers: { Authorization: `Bearer ${currentToken()}` },
      });
      setDeleting(null);
      setNotice(`Đã xóa danh mục ${deleting.name}.`);
      await reload();
      requestAnimationFrame(() => listHeadingRef.current?.focus());
    } catch (cause) {
      setDeleteError(errorMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="category-admin">
      <div className="category-admin-heading">
        <div>
          <h1 id="category-list-title" className="rise" ref={listHeadingRef} tabIndex={-1}>Quản lý danh mục</h1>
          <p>Sắp xếp công thức theo nhóm để người đọc dễ duyệt.</p>
        </div>
        <button type="button" className="category-admin-link-button" onClick={() => void reload()} disabled={loading}>Tải lại</button>
      </div>
      {listError && <Alert variant="error">{listError}</Alert>}
      {notice && <Alert variant="success">{notice}</Alert>}

      <section className="category-admin-card" aria-labelledby="category-form-title">
        <h2 id="category-form-title">Thêm danh mục</h2>
        {formError && !editing && <Alert variant="error" ref={summaryRef} tabIndex={-1}>{formError}</Alert>}
        <form className="category-admin-add" onSubmit={(event) => void save(event)} noValidate>
          <FormField label="Tên danh mục" error={editing ? undefined : fieldErrors.name} hint="2–50 ký tự. Đường dẫn tự sinh từ tên." required>
            {(control) => <input {...control} value={editing ? "" : form.name} disabled={Boolean(editing)} onChange={(event) => setForm({ ...form, name: event.target.value })} minLength={2} maxLength={50} required />}
          </FormField>
          <FormField label="Mô tả (không bắt buộc)" error={editing ? undefined : fieldErrors.description}>
            {(control) => <input {...control} value={editing ? "" : form.description} disabled={Boolean(editing)} onChange={(event) => setForm({ ...form, description: event.target.value })} maxLength={2000} placeholder="Sinh tố, trà, cà phê…" />}
          </FormField>
          <button type="submit" className="category-admin-primary" disabled={busy || Boolean(editing)}>{busy && !editing ? "Đang lưu…" : "Thêm danh mục"}</button>
        </form>
        <p className="category-admin-help">Lưu danh mục trước, rồi bấm Sửa để tải ảnh lên.</p>
      </section>

      <section className="category-admin-list-section" aria-labelledby="category-table-title">
        <h2 id="category-table-title">Tất cả danh mục <small>· {categories.length}</small></h2>
        {loading ? <SkeletonList label="Đang tải danh mục" /> : !listError && categories.length === 0 ? (
          <p className="category-admin-empty">Chưa có danh mục nào. Hãy tạo danh mục đầu tiên.</p>
        ) : categories.length > 0 && (
          <div className="category-admin-table-wrap" role="region" aria-label="Bảng danh mục" tabIndex={0}>
            <table className="category-admin-table">
              <caption className="visually-hidden">Danh sách danh mục công thức</caption>
              <thead>
                <tr>
                  <th scope="col">Danh mục</th>
                  <th scope="col" className="category-admin-slug">Đường dẫn</th>
                  <th scope="col">Công thức</th>
                  <th scope="col" className="category-admin-actions-head">Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {categories.map((category) => {
                  if (editing?.id === category.id) {
                    return (
                      <tr key={category.id} className="category-admin-editing">
                        <td colSpan={4}>
                          {formError && <Alert variant="error" ref={summaryRef} tabIndex={-1}>{formError}</Alert>}
                          <form className="category-admin-edit-form" onSubmit={(event) => void save(event)} noValidate>
                            <FormField label="Tên danh mục" error={fieldErrors.name} hint="Đường dẫn không đổi khi đổi tên." required>
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
                            <div className="category-admin-actions">
                              <button type="button" onClick={cancelEdit} disabled={busy}>Hủy</button>
                              <button type="submit" className="category-admin-primary" disabled={busy}>{busy ? "Đang lưu…" : "Lưu"}</button>
                            </div>
                          </form>
                        </td>
                      </tr>
                    );
                  }

                  if (deleting?.id === category.id) {
                    return (
                      <tr key={category.id} className="category-admin-deleting">
                        <td colSpan={4}>
                          <div role="alertdialog" aria-label={`Xác nhận xóa danh mục ${category.name}`}>
                            <span>
                              <strong>Xóa danh mục “{category.name}”?</strong>
                              Danh mục chưa có công thức nào. Thao tác này không thể hoàn tác.
                            </span>
                            <span className="category-admin-actions">
                              <button type="button" onClick={cancelDelete} disabled={busy}>Giữ lại</button>
                              <button type="button" className="category-admin-danger" onClick={() => void confirmDelete()} disabled={busy}>{busy ? "Đang xóa…" : "Xóa danh mục"}</button>
                            </span>
                          </div>
                          {deleteError && <Alert variant="error">{deleteError}</Alert>}
                        </td>
                      </tr>
                    );
                  }

                  return (
                    <tr key={category.id}>
                      <td>
                        <div className="category-admin-name">
                          {category.imageUrl ? (
                            <Image src={category.imageUrl} alt="" width={48} height={32} unoptimized className="category-admin-thumb" />
                          ) : (
                            <span className="category-admin-thumb category-admin-thumb-empty" aria-hidden="true" />
                          )}
                          <span>
                            <strong>{category.name}</strong>
                            <span className="category-admin-slug-inline">{category.slug}</span>
                          </span>
                        </div>
                      </td>
                      <td className="category-admin-slug"><code>{category.slug}</code></td>
                      <td><span className="category-admin-count">{category.recipeCount} công thức</span></td>
                      <td>
                        <div className="category-admin-row-actions">
                          <button type="button" className="category-admin-edit" onClick={(event) => beginEdit(category, event.currentTarget)} aria-label={`Sửa ${category.name}`}>Sửa</button>
                          {/* aria-disabled (not disabled) keeps the button focusable so the reason is reachable by keyboard and screen reader. */}
                          <button
                            type="button"
                            className="category-admin-delete"
                            disabled={busy}
                            aria-disabled={category.recipeCount > 0 ? true : undefined}
                            aria-describedby={category.recipeCount > 0 ? `delete-blocked-${category.id}` : undefined}
                            onClick={(event) => beginDelete(category, event.currentTarget)}
                            aria-label={`Xóa ${category.name}`}
                          >
                            Xóa
                          </button>
                          {category.recipeCount > 0 && (
                            <span id={`delete-blocked-${category.id}`} className="category-admin-why">
                              Còn {category.recipeCount} công thức nên chưa xóa được
                            </span>
                          )}
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}
