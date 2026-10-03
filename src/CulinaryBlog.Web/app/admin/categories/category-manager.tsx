"use client";

import { useCallback, useEffect, useState, type FormEvent } from "react";
import Link from "next/link";
import { ApiError, apiFetch } from "@/lib/api/client";
import type { CategoryDto } from "@/lib/api/contracts";
import "./categories.css";

type FormValues = { name: string; description: string; imageUrl: string; orderIndex: string };
const emptyForm: FormValues = { name: "", description: "", imageUrl: "", orderIndex: "0" };

function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 401 || error.status === 403) return "Phiên quản trị không hợp lệ hoặc không có quyền Admin.";
    if (error.status === 409) return "Tên danh mục đã tồn tại. Vui lòng chọn tên khác.";
    if (error.status === 422) return "Dữ liệu chưa hợp lệ. Vui lòng kiểm tra các trường trong biểu mẫu.";
    return error.message;
  }
  return "Không thể kết nối API. Vui lòng thử lại.";
}

export default function CategoryManager() {
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [token, setToken] = useState("");
  const [editing, setEditing] = useState<CategoryDto | null>(null);
  const [form, setForm] = useState<FormValues>(emptyForm);

  const reload = useCallback(async () => {
    setLoading(true);
    try {
      setCategories(await apiFetch<CategoryDto[]>("/categories"));
      setError("");
    } catch (cause) {
      setError(errorMessage(cause));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { void reload(); }, [reload]);

  function beginEdit(category: CategoryDto) {
    setEditing(category);
    setForm({ name: category.name, description: category.description ?? "", imageUrl: category.imageUrl ?? "", orderIndex: String(category.orderIndex) });
    setError("");
    setNotice("");
  }

  function cancelEdit() {
    setEditing(null);
    setForm(emptyForm);
    setError("");
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setNotice("");
    const name = form.name.trim();
    if (name.length < 2 || name.length > 50 || /[<>]/.test(name) || !/[a-zA-Z\p{L}\p{N}]/u.test(name)) {
      setError("Tên cần từ 2 đến 50 ký tự, không chứa HTML và có ít nhất một chữ hoặc số.");
      return;
    }
    if (!token.trim()) {
      setError("Nhập access token của tài khoản Admin để lưu danh mục.");
      return;
    }
    setBusy(true);
    try {
      const path = editing ? `/categories/${editing.id}` : "/categories";
      await apiFetch<CategoryDto>(path, {
        method: editing ? "PUT" : "POST",
        headers: { "Content-Type": "application/json", Authorization: `Bearer ${token.trim()}` },
        body: JSON.stringify({ name, description: form.description.trim() || null, imageUrl: form.imageUrl.trim() || null, orderIndex: Number(form.orderIndex) }),
      });
      setNotice(editing ? "Đã cập nhật danh mục." : "Đã tạo danh mục.");
      setEditing(null);
      setForm(emptyForm);
      await reload();
    } catch (cause) {
      setError(errorMessage(cause));
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
        <p>Thêm và cập nhật danh mục để tổ chức công thức cho người đọc.</p>
      </div>
      <div className="category-admin-grid">
        <section className="category-admin-card" aria-labelledby="category-list-title">
          <div className="category-admin-section-heading">
            <h2 id="category-list-title">Danh sách danh mục</h2>
            <button type="button" className="category-admin-link-button" onClick={() => void reload()} disabled={loading}>Tải lại</button>
          </div>
          {loading ? <p role="status">Đang tải danh mục…</p> : categories.length === 0 ? <p>Chưa có danh mục nào. Hãy tạo danh mục đầu tiên.</p> : (
            <ul className="category-admin-list">
              {categories.map((category) => (
                <li key={category.id}>
                  <div><strong>{category.name}</strong><span>/{category.slug} · {category.recipeCount} công thức</span></div>
                  <button type="button" onClick={() => beginEdit(category)} aria-label={`Sửa ${category.name}`}>Sửa</button>
                </li>
              ))}
            </ul>
          )}
        </section>
        <section className="category-admin-card" aria-labelledby="category-form-title">
          <h2 id="category-form-title">{editing ? "Cập nhật danh mục" : "Thêm danh mục"}</h2>
          <p className="category-admin-help">Thao tác ghi yêu cầu access token của tài khoản Admin. Token chỉ được giữ trong trang hiện tại.</p>
          {error && <p className="category-admin-error" role="alert">{error}</p>}
          {notice && <p className="category-admin-success" role="status">{notice}</p>}
          <form onSubmit={(event) => void save(event)}>
            <label htmlFor="category-token">Admin access token</label>
            <input id="category-token" type="password" autoComplete="off" value={token} onChange={(event) => setToken(event.target.value)} required />
            <label htmlFor="category-name">Tên danh mục</label>
            <input id="category-name" value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} minLength={2} maxLength={50} required />
            <label htmlFor="category-description">Mô tả</label>
            <textarea id="category-description" value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} maxLength={2000} rows={4} />
            <label htmlFor="category-image">URL hình ảnh</label>
            <input id="category-image" type="url" value={form.imageUrl} onChange={(event) => setForm({ ...form, imageUrl: event.target.value })} maxLength={500} />
            <label htmlFor="category-order">Thứ tự hiển thị</label>
            <input id="category-order" type="number" min={0} value={form.orderIndex} onChange={(event) => setForm({ ...form, orderIndex: event.target.value })} required />
            <div className="category-admin-actions">
              <button type="submit" className="category-admin-primary" disabled={busy}>{busy ? "Đang lưu…" : editing ? "Lưu thay đổi" : "Tạo danh mục"}</button>
              {editing && <button type="button" onClick={cancelEdit} disabled={busy}>Hủy</button>}
            </div>
          </form>
        </section>
      </div>
    </main>
  );
}
