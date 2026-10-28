"use client";

import { useCallback, useEffect, useRef, useState, type FormEvent } from "react";
import { ApiError, apiFetch } from "@/lib/api/client";
import type { CategoryDto } from "@/lib/api/contracts";
import { Alert, FormField, SkeletonList } from "@/components/shared";
import "./categories.css";

type FormValues = { name: string; description: string; imageUrl: string; orderIndex: string };
type FieldName = keyof FormValues;
type FieldErrors = Partial<Record<FieldName, string>>;

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
  const [token, setToken] = useState("");
  const [editing, setEditing] = useState<CategoryDto | null>(null);
  const [deleting, setDeleting] = useState<CategoryDto | null>(null);
  const [form, setForm] = useState<FormValues>(emptyForm);
  const dialogRef = useRef<HTMLDialogElement>(null);
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
  useEffect(() => {
    if (deleting && dialogRef.current && !dialogRef.current.open) dialogRef.current.showModal();
  }, [deleting]);

  function showFormError(message: string, fields: FieldErrors = {}) {
    setFormError(message);
    setFieldErrors(fields);
    requestAnimationFrame(() => summaryRef.current?.focus());
  }

  function beginEdit(category: CategoryDto, trigger: HTMLButtonElement) {
    editTriggerRef.current = trigger;
    setEditing(category);
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
    if (!token.trim()) {
      showFormError("Nhập access token của tài khoản Admin để lưu danh mục.");
      return;
    }
    setBusy(true);
    try {
      const path = editing ? `/categories/${editing.id}` : "/categories";
      await apiFetch<CategoryDto>(path, {
        method: editing ? "PUT" : "POST",
        headers: { "Content-Type": "application/json", Authorization: `Bearer ${token.trim()}` },
        body: JSON.stringify({
          name: form.name.trim(), description: form.description.trim() || null,
          imageUrl: form.imageUrl.trim() || null, orderIndex: Number(form.orderIndex),
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
    if (!token.trim()) {
      showFormError("Nhập access token của tài khoản Admin trước khi xóa danh mục.");
      return;
    }
    deleteTriggerRef.current = trigger;
    setDeleteError("");
    setNotice("");
    setDeleting(category);
  }

  function cancelDelete() {
    dialogRef.current?.close();
    setDeleting(null);
    setDeleteError("");
    requestAnimationFrame(() => deleteTriggerRef.current?.focus());
  }

  async function confirmDelete() {
    if (!deleting) return;
    setBusy(true);
    setDeleteError("");
    try {
      await apiFetch<void>(`/categories/${deleting.id}`, {
        method: "DELETE", headers: { Authorization: `Bearer ${token.trim()}` },
      });
      dialogRef.current?.close();
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
      <div className="category-admin-main">
        <section className="category-admin-list-section" aria-labelledby="category-list-title">
          <div className="category-admin-section-heading">
            <h1 id="category-list-title" ref={listHeadingRef} tabIndex={-1}>Danh mục</h1>
            <button type="button" className="category-admin-link-button" onClick={() => void reload()} disabled={loading}>Tải lại</button>
          </div>
          {listError && <Alert variant="error">{listError}</Alert>}
          {notice && <Alert variant="success">{notice}</Alert>}
          {loading ? <SkeletonList label="Đang tải danh mục" /> : !listError && categories.length === 0 ? (
            <p className="category-admin-empty">Chưa có danh mục nào. Hãy tạo danh mục đầu tiên.</p>
          ) : categories.length > 0 && (
            <div className="category-admin-table-wrap" role="region" aria-label="Bảng danh mục" tabIndex={0}>
              <table className="category-admin-table">
                <caption className="visually-hidden">Danh sách danh mục công thức</caption>
                <thead>
                  <tr>
                    <th scope="col">Tên</th>
                    <th scope="col" className="category-admin-slug">Slug</th>
                    <th scope="col">Số công thức</th>
                    <th scope="col"><span className="visually-hidden">Thao tác</span></th>
                  </tr>
                </thead>
                <tbody>
                  {categories.map((category) => (
                    <tr key={category.id}>
                      <td>
                        <strong>{category.name}</strong>
                        <span className="category-admin-slug-inline">{category.slug}</span>
                      </td>
                      <td className="category-admin-slug">{category.slug}</td>
                      <td>{category.recipeCount}</td>
                      <td className="category-admin-row-actions">
                        <button type="button" className="category-admin-edit" onClick={(event) => beginEdit(category, event.currentTarget)} aria-label={`Sửa ${category.name}`}>Sửa</button>
                        {/* aria-disabled (not disabled) keeps the button focusable so the reason is reachable by keyboard and screen reader. */}
                        <button
                          type="button"
                          className="category-admin-delete"
                          disabled={busy}
                          aria-disabled={category.recipeCount > 0 ? true : undefined}
                          aria-describedby={category.recipeCount > 0 ? `delete-blocked-${category.id}` : undefined}
                          title={category.recipeCount > 0 ? `Danh mục còn ${category.recipeCount} công thức` : "Xóa danh mục"}
                          onClick={(event) => beginDelete(category, event.currentTarget)}
                          aria-label={`Xóa ${category.name}`}
                        >
                          Xóa
                        </button>
                        {category.recipeCount > 0 && (
                          <span id={`delete-blocked-${category.id}`} className="visually-hidden">
                            Không thể xóa vì danh mục còn {category.recipeCount} công thức.
                          </span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          <p className="category-admin-note">Chỉ xóa được danh mục không còn công thức. Slug giữ nguyên khi đổi tên.</p>
        </section>
        <section className="category-admin-card" aria-labelledby="category-form-title">
          <h2 id="category-form-title">{editing ? "Cập nhật danh mục" : "Thêm danh mục"}</h2>
          <p className="category-admin-help">Thao tác ghi yêu cầu access token của tài khoản Admin. Token chỉ được giữ trong trang hiện tại.</p>
          {formError && <Alert variant="error" ref={summaryRef} tabIndex={-1}>{formError}</Alert>}
          <form onSubmit={(event) => void save(event)} noValidate>
            <FormField label="Admin access token" required>
              {(control) => <input {...control} type="password" autoComplete="off" value={token} onChange={(event) => setToken(event.target.value)} required />}
            </FormField>
            <FormField label="Tên" error={fieldErrors.name} hint="2–50 ký tự. Slug sẽ tự sinh, ví dụ “mon-chinh”." required>
              {(control) => <input {...control} ref={nameInputRef} value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} minLength={2} maxLength={50} required />}
            </FormField>
            <FormField label="Mô tả" error={fieldErrors.description}>
              {(control) => <textarea {...control} value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} maxLength={2000} rows={3} />}
            </FormField>
            <FormField label="URL hình ảnh" error={fieldErrors.imageUrl}>
              {(control) => <input {...control} type="url" value={form.imageUrl} onChange={(event) => setForm({ ...form, imageUrl: event.target.value })} maxLength={500} />}
            </FormField>
            <FormField label="Thứ tự hiển thị" error={fieldErrors.orderIndex} required>
              {(control) => <input {...control} type="number" min={0} step={1} value={form.orderIndex} onChange={(event) => setForm({ ...form, orderIndex: event.target.value })} required />}
            </FormField>
            <div className="category-admin-actions">
              <button type="submit" className="category-admin-primary" disabled={busy}>{busy ? "Đang lưu…" : editing ? "Lưu thay đổi" : "Tạo danh mục"}</button>
              {editing && <button type="button" onClick={cancelEdit} disabled={busy}>Hủy</button>}
            </div>
          </form>
        </section>
      </div>
      {deleting && (
        <dialog ref={dialogRef} className="category-admin-dialog" aria-labelledby="delete-category-title" aria-describedby="delete-category-description" onCancel={(event) => { event.preventDefault(); if (!busy) cancelDelete(); }}>
          <h2 id="delete-category-title">Xóa danh mục?</h2>
          <p id="delete-category-description">Bạn sắp xóa “{deleting.name}”. Danh mục sẽ không còn xuất hiện trong danh sách. Thao tác này không thể hoàn tác trên giao diện.</p>
          {deleteError && <Alert variant="error">{deleteError}</Alert>}
          <div className="category-admin-actions">
            <button type="button" onClick={cancelDelete} disabled={busy}>Giữ lại</button>
            <button type="button" className="category-admin-danger" onClick={() => void confirmDelete()} disabled={busy}>{busy ? "Đang xóa…" : "Xác nhận xóa"}</button>
          </div>
        </dialog>
      )}
    </div>
  );
}
