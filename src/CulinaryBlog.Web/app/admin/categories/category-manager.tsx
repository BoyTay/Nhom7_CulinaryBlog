"use client";

import Link from "next/link";
import { useCallback, useEffect, useRef, useState, type FormEvent } from "react";
import { ApiError, apiFetch } from "@/lib/api/client";
import type { CategoryDto } from "@/lib/api/contracts";
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

  function beginEdit(category: CategoryDto) {
    setEditing(category);
    setForm({ name: category.name, description: category.description ?? "", imageUrl: category.imageUrl ?? "", orderIndex: String(category.orderIndex) });
    setFormError("");
    setFieldErrors({});
    setNotice("");
  }

  function cancelEdit() {
    setEditing(null);
    setForm(emptyForm);
    setFormError("");
    setFieldErrors({});
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
    } catch (cause) {
      showFormError(errorMessage(cause), serverFieldErrors(cause));
    } finally {
      setBusy(false);
    }
  }

  function beginDelete(category: CategoryDto, trigger: HTMLButtonElement) {
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
    <main className="category-admin">
      <header className="category-admin-header">
        <Link href="/" className="category-admin-brand">Culinary Blog</Link>
        <span>Quản trị nội dung</span>
      </header>
      <div className="category-admin-intro">
        <p className="kicker">Danh mục công thức</p>
        <h1>Quản lý danh mục</h1>
        <p>Thêm, cập nhật và xóa danh mục để tổ chức công thức cho người đọc.</p>
      </div>
      <div className="category-admin-grid">
        <section className="category-admin-card" aria-labelledby="category-list-title">
          <div className="category-admin-section-heading">
            <h2 id="category-list-title" ref={listHeadingRef} tabIndex={-1}>Danh sách danh mục</h2>
            <button type="button" className="category-admin-link-button" onClick={() => void reload()} disabled={loading}>Tải lại</button>
          </div>
          {listError && <p className="category-admin-error" role="alert">{listError}</p>}
          {notice && <p className="category-admin-success" role="status">{notice}</p>}
          {loading ? <p role="status">Đang tải danh mục…</p> : !listError && categories.length === 0 ? <p>Chưa có danh mục nào. Hãy tạo danh mục đầu tiên.</p> : (
            <ul className="category-admin-list">
              {categories.map((category) => (
                <li key={category.id}>
                  <div>
                    <strong>{category.name}</strong>
                    <span>/{category.slug} · {category.recipeCount} công thức</span>
                    {category.recipeCount > 0 && <span>Cần chuyển hoặc xóa công thức trước khi xóa danh mục.</span>}
                  </div>
                  <div className="category-admin-row-actions">
                    <button type="button" onClick={() => beginEdit(category)} aria-label={`Sửa ${category.name}`}>Sửa</button>
                    <button type="button" disabled={category.recipeCount > 0 || busy} onClick={(event) => beginDelete(category, event.currentTarget)} aria-label={`Xóa ${category.name}`}>Xóa</button>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </section>
        <section className="category-admin-card" aria-labelledby="category-form-title">
          <h2 id="category-form-title">{editing ? "Cập nhật danh mục" : "Thêm danh mục"}</h2>
          <p className="category-admin-help">Thao tác ghi yêu cầu access token của tài khoản Admin. Token chỉ được giữ trong trang hiện tại.</p>
          {formError && <div className="category-admin-error" role="alert" ref={summaryRef} tabIndex={-1}>{formError}</div>}
          <form onSubmit={(event) => void save(event)} noValidate>
            <label htmlFor="category-token">Admin access token</label>
            <input id="category-token" type="password" autoComplete="off" value={token} onChange={(event) => setToken(event.target.value)} required />
            <label htmlFor="category-name">Tên danh mục</label>
            <input id="category-name" value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} minLength={2} maxLength={50} required aria-invalid={!!fieldErrors.name} aria-describedby={fieldErrors.name ? "category-name-error" : undefined} />
            {fieldErrors.name && <span id="category-name-error" className="category-admin-field-error">{fieldErrors.name}</span>}
            <label htmlFor="category-description">Mô tả</label>
            <textarea id="category-description" value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} maxLength={2000} rows={4} aria-invalid={!!fieldErrors.description} aria-describedby={fieldErrors.description ? "category-description-error" : undefined} />
            {fieldErrors.description && <span id="category-description-error" className="category-admin-field-error">{fieldErrors.description}</span>}
            <label htmlFor="category-image">URL hình ảnh</label>
            <input id="category-image" type="url" value={form.imageUrl} onChange={(event) => setForm({ ...form, imageUrl: event.target.value })} maxLength={500} aria-invalid={!!fieldErrors.imageUrl} aria-describedby={fieldErrors.imageUrl ? "category-image-error" : undefined} />
            {fieldErrors.imageUrl && <span id="category-image-error" className="category-admin-field-error">{fieldErrors.imageUrl}</span>}
            <label htmlFor="category-order">Thứ tự hiển thị</label>
            <input id="category-order" type="number" min={0} step={1} value={form.orderIndex} onChange={(event) => setForm({ ...form, orderIndex: event.target.value })} required aria-invalid={!!fieldErrors.orderIndex} aria-describedby={fieldErrors.orderIndex ? "category-order-error" : undefined} />
            {fieldErrors.orderIndex && <span id="category-order-error" className="category-admin-field-error">{fieldErrors.orderIndex}</span>}
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
          {deleteError && <p className="category-admin-error" role="alert">{deleteError}</p>}
          <div className="category-admin-actions">
            <button type="button" onClick={cancelDelete} disabled={busy}>Giữ lại</button>
            <button type="button" className="category-admin-danger" onClick={() => void confirmDelete()} disabled={busy}>{busy ? "Đang xóa…" : "Xác nhận xóa"}</button>
          </div>
        </dialog>
      )}
    </main>
  );
}
