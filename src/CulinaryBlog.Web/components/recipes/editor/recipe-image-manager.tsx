"use client";

import {
  useRef,
  useState,
  type ChangeEvent,
} from "react";
import { Alert } from "@/components/shared";
import { RecipeImage } from "@/components/recipes/recipe-image";
import { ApiError } from "@/lib/api/client";
import { authStore } from "@/lib/auth/auth-store";
import { recipeClient } from "@/lib/recipes/client";
import type { RecipeImageDto } from "@/lib/recipes/contracts";
import { MissingRecipeEtagError, recipeEditorConflictKind } from "@/lib/recipes/editor";
import {
  deleteRecipeImageAndRefresh,
  RecipeImageDeletedRefreshError,
  RecipeImageUploadCancelledError,
  setPrimaryRecipeImage,
  uploadRecipeImageWithProgress,
  validateRecipeImageFile,
  type RecipeImageUploadTask,
} from "@/lib/recipes/images";

interface RecipeImageManagerProps {
  recipeId: string | null;
  eTag: string | null;
  images: RecipeImageDto[];
  onETagChange: (eTag: string) => void;
  onImagesChange: (images: RecipeImageDto[]) => void;
}

export function RecipeImageManager({
  recipeId,
  eTag,
  images,
  onETagChange,
  onImagesChange,
}: RecipeImageManagerProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const uploadTaskRef = useRef<RecipeImageUploadTask | null>(null);
  const [file, setFile] = useState<File | null>(null);
  const [altText, setAltText] = useState("");
  const [isPrimary, setIsPrimary] = useState(false);
  const [progress, setProgress] = useState<number | null>(null);
  const [busyImageId, setBusyImageId] = useState<string | null>(null);
  const [confirmDeleteId, setConfirmDeleteId] = useState<string | null>(null);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [mustRefresh, setMustRefresh] = useState(false);

  const ready = Boolean(recipeId && eTag && !mustRefresh);

  function selectFile(event: ChangeEvent<HTMLInputElement>) {
    const selected = event.target.files?.[0] ?? null;
    setMessage("");
    setError("");
    if (!selected) {
      setFile(null);
      return;
    }
    const validationError = validateRecipeImageFile(selected);
    if (validationError) {
      setFile(null);
      setError(validationError);
      event.target.value = "";
      return;
    }
    setFile(selected);
  }

  async function upload() {
    setError("");
    setMessage("");
    if (!recipeId || !eTag || !file) return;
    if (altText.trim().length > 200) {
      setError("Mô tả ảnh không được vượt quá 200 ký tự.");
      return;
    }
    const accessToken = authStore.getAccessToken();
    if (!accessToken) {
      setError("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại trước khi tải ảnh.");
      return;
    }

    setProgress(0);
    const task = uploadRecipeImageWithProgress({
      recipeId,
      file,
      altText,
      isPrimary,
      eTag,
      accessToken,
      onProgress: setProgress,
    });
    uploadTaskRef.current = task;
    try {
      const response = await task.promise;
      const nextImages = response.data.isPrimary
        ? images.map((image) => ({ ...image, isPrimary: false })).concat(response.data)
        : images.concat(response.data);
      onImagesChange(nextImages);
      onETagChange(response.eTag!);
      setFile(null);
      setAltText("");
      setIsPrimary(false);
      if (fileInputRef.current) fileInputRef.current.value = "";
      setMessage("Ảnh đã được tải lên.");
    } catch (caught) {
      if (caught instanceof RecipeImageUploadCancelledError) {
        setMessage("Đã hủy tải ảnh. Tệp đã chọn vẫn được giữ để bạn thử lại.");
      } else {
        showMutationError(caught, setError, setMustRefresh);
      }
    } finally {
      uploadTaskRef.current = null;
      setProgress(null);
    }
  }

  async function setPrimary(imageId: string) {
    if (!recipeId || !eTag) return;
    setBusyImageId(imageId);
    setError("");
    setMessage("");
    try {
      const result = await setPrimaryRecipeImage(recipeClient, recipeId, imageId, eTag, images);
      onImagesChange(result.images);
      onETagChange(result.eTag);
      setMessage("Đã đặt ảnh đại diện.");
    } catch (caught) {
      showMutationError(caught, setError, setMustRefresh);
    } finally {
      setBusyImageId(null);
    }
  }

  async function remove(imageId: string) {
    if (!recipeId || !eTag) return;
    setBusyImageId(imageId);
    setConfirmDeleteId(null);
    setError("");
    setMessage("");
    try {
      const result = await deleteRecipeImageAndRefresh(recipeClient, recipeId, imageId, eTag);
      onImagesChange(result.images);
      onETagChange(result.eTag);
      setMessage("Đã xóa ảnh và đồng bộ phiên bản công thức mới nhất.");
    } catch (caught) {
      if (caught instanceof RecipeImageDeletedRefreshError) {
        onImagesChange(images.filter((image) => image.id !== caught.imageId));
        setMustRefresh(true);
        setError("Ảnh đã được xóa nhưng chưa lấy được phiên bản mới. Hãy tải lại danh sách ảnh trước khi thao tác tiếp.");
      } else {
        showMutationError(caught, setError, setMustRefresh);
      }
    } finally {
      setBusyImageId(null);
    }
  }

  async function refresh() {
    if (!recipeId) return;
    setError("");
    setMessage("");
    try {
      const detail = await recipeClient.detail(recipeId);
      if (!detail.eTag) throw new MissingRecipeEtagError();
      onImagesChange(detail.data.images);
      onETagChange(detail.eTag);
      setMustRefresh(false);
      setMessage("Đã tải danh sách ảnh và phiên bản mới nhất.");
    } catch (caught) {
      setError(imageErrorMessage(caught));
    }
  }

  return (
    <section className="recipe-image-manager" aria-labelledby="recipe-images-heading">
      <div className="recipe-image-heading">
        <div>
          <h2 id="recipe-images-heading">Ảnh công thức</h2>
          <p>JPEG, PNG, WebP hoặc AVIF · tối đa 5 MiB mỗi ảnh.</p>
        </div>
        {recipeId && (
          <button className="recipe-image-text-button" type="button" onClick={() => void refresh()} disabled={progress !== null || busyImageId !== null}>
            Tải lại ảnh
          </button>
        )}
      </div>

      {!recipeId ? (
        <div className="recipe-editor-upload-placeholder">
          <strong>Lưu bản nháp trước khi tải ảnh</strong>
          <span>Nhấn “Tạo bản nháp”; trình chỉnh sửa sẽ mở lại để bạn thêm ảnh an toàn.</span>
        </div>
      ) : (
        <div className="recipe-image-upload-box">
          <label htmlFor="recipe-image-file">Chọn tệp ảnh</label>
          <input
            ref={fileInputRef}
            id="recipe-image-file"
            type="file"
            accept="image/jpeg,image/png,image/webp,image/avif"
            onChange={selectFile}
            disabled={!ready || progress !== null}
          />
          <label htmlFor="recipe-image-alt">Mô tả ảnh <span>(không bắt buộc)</span></label>
          <input
            id="recipe-image-alt"
            type="text"
            maxLength={200}
            value={altText}
            onChange={(event) => setAltText(event.target.value)}
            disabled={!ready || progress !== null}
            placeholder="Ví dụ: Tô phở bò đã hoàn thành"
          />
          <label className="recipe-image-checkbox">
            <input type="checkbox" checked={isPrimary} onChange={(event) => setIsPrimary(event.target.checked)} disabled={!ready || progress !== null} />
            Dùng làm ảnh đại diện
          </label>
          {file && <p className="recipe-image-selected">Đã chọn: <strong>{file.name}</strong></p>}
          {progress !== null && (
            <div className="recipe-image-progress" aria-live="polite">
              <div><span>Đang tải ảnh…</span><strong>{progress}%</strong></div>
              <progress max={100} value={progress}>{progress}%</progress>
            </div>
          )}
          <div className="recipe-image-upload-actions">
            <button type="button" className="recipe-editor-button recipe-editor-button-primary" onClick={() => void upload()} disabled={!ready || !file || progress !== null}>
              Tải ảnh lên
            </button>
            {progress !== null && (
              <button type="button" className="recipe-editor-button recipe-editor-button-secondary" onClick={() => uploadTaskRef.current?.cancel()}>
                Hủy tải
              </button>
            )}
          </div>
        </div>
      )}

      {(error || message) && (
        <div className="recipe-image-feedback" aria-live="polite">
          {error && (
            <Alert variant="error" title="Chưa thể cập nhật ảnh" action={mustRefresh ? <button type="button" onClick={() => void refresh()}>Tải lại ảnh</button> : undefined}>
              {error}
            </Alert>
          )}
          {message && <Alert variant="success">{message}</Alert>}
        </div>
      )}

      {images.length > 0 && (
        <ul className="recipe-image-list" aria-label="Ảnh đã tải lên">
          {images.map((image) => (
            <li key={image.id}>
              <RecipeEditorImage image={image} />
              <div className="recipe-image-meta">
                <strong>{image.isPrimary ? "Ảnh đại diện" : "Ảnh công thức"}</strong>
                <span>{image.altText || "Chưa có mô tả ảnh"}</span>
              </div>
              <div className="recipe-image-row-actions">
                {!image.isPrimary && (
                  <button type="button" onClick={() => void setPrimary(image.id)} disabled={!ready || busyImageId !== null}>
                    Đặt đại diện
                  </button>
                )}
                {confirmDeleteId === image.id ? (
                  <>
                    <button className="danger" type="button" onClick={() => void remove(image.id)} disabled={!ready || busyImageId !== null}>Xác nhận xóa</button>
                    <button type="button" onClick={() => setConfirmDeleteId(null)} disabled={busyImageId !== null}>Giữ ảnh</button>
                  </>
                ) : (
                  <button className="danger" type="button" onClick={() => setConfirmDeleteId(image.id)} disabled={!ready || busyImageId !== null}>Xóa</button>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

function RecipeEditorImage({ image }: { image: RecipeImageDto }) {
  return (
    <RecipeImage
      alt={image.altText || "Ảnh công thức"}
      className="recipe-image-preview"
      height={84}
      image={image}
      sizes="112px"
      variant="thumbnail"
      width={112}
    />
  );
}

function showMutationError(
  error: unknown,
  setError: (message: string) => void,
  setMustRefresh: (value: boolean) => void,
) {
  if (recipeEditorConflictKind(error) || error instanceof MissingRecipeEtagError) setMustRefresh(true);
  setError(imageErrorMessage(error));
}

function imageErrorMessage(error: unknown): string {
  if (recipeEditorConflictKind(error)) return "Công thức đã thay đổi ở nơi khác. Hãy tải lại ảnh trước khi tiếp tục.";
  if (error instanceof MissingRecipeEtagError) return "Máy chủ không trả về phiên bản công thức mới. Hãy tải lại ảnh trước khi tiếp tục.";
  if (error instanceof ApiError) {
    if (error.status === 401) return "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
    if (error.status === 403) return "Bạn không có quyền thay đổi ảnh của công thức này.";
    if (error.status === 413) return "Ảnh vượt quá giới hạn dung lượng 5 MiB.";
    if (error.status === 415) return "Định dạng ảnh không được hỗ trợ.";
    if (error.status === 422) return error.problem.detail ?? "Tệp ảnh không hợp lệ.";
    return error.problem.detail ?? error.problem.title ?? "Không thể cập nhật ảnh.";
  }
  return "Không thể kết nối máy chủ. Vui lòng thử lại.";
}
