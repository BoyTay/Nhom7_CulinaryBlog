import { ApiError } from "../api/client.ts";
import type { ApiProblemDetails } from "../api/contracts.ts";
import type { RecipeClient } from "./client.ts";
import type {
  RecipeApiResponse,
  RecipeImageDto,
} from "./contracts.ts";
import { MissingRecipeEtagError } from "./editor.ts";

export const MAX_RECIPE_IMAGE_BYTES = 5 * 1024 * 1024;
export const RECIPE_IMAGE_MIME_TYPES = [
  "image/jpeg",
  "image/png",
  "image/webp",
  "image/avif",
] as const;

export type RecipeImageFile = Pick<File, "name" | "size" | "type">;

export function validateRecipeImageFile(file: RecipeImageFile): string | null {
  if (!RECIPE_IMAGE_MIME_TYPES.includes(file.type as typeof RECIPE_IMAGE_MIME_TYPES[number])) {
    return "Chỉ chấp nhận ảnh JPEG, PNG, WebP hoặc AVIF.";
  }
  if (file.size <= 0) return "Tệp ảnh không được để trống.";
  if (file.size > MAX_RECIPE_IMAGE_BYTES) return "Ảnh phải có dung lượng không quá 5 MiB.";
  return null;
}

interface RecipeImageXhrUpload {
  onprogress: ((event: ProgressEvent) => void) | null;
}

export interface RecipeImageXhr {
  readonly upload: RecipeImageXhrUpload;
  status: number;
  responseText: string;
  withCredentials: boolean;
  onload: (() => void) | null;
  onerror: (() => void) | null;
  onabort: (() => void) | null;
  open(method: string, url: string): void;
  setRequestHeader(name: string, value: string): void;
  getResponseHeader(name: string): string | null;
  send(body: FormData): void;
  abort(): void;
}

export interface RecipeImageUploadOptions {
  recipeId: string;
  file: File;
  altText: string;
  isPrimary: boolean;
  eTag: string;
  accessToken: string;
  baseUrl?: string;
  onProgress?: (percentage: number) => void;
  xhrFactory?: () => RecipeImageXhr;
}

export interface RecipeImageUploadTask {
  promise: Promise<RecipeApiResponse<RecipeImageDto>>;
  cancel(): void;
}

export class RecipeImageUploadCancelledError extends Error {
  constructor() {
    super("The recipe image upload was cancelled.");
  }
}

export class RecipeImageDeletedRefreshError extends Error {
  readonly imageId: string;

  constructor(imageId: string, cause: unknown) {
    super("The image was deleted but the fresh recipe ETag could not be loaded.", { cause });
    this.imageId = imageId;
  }
}

function parseJson<T>(value: string): T | null {
  if (!value) return null;
  try {
    return JSON.parse(value) as T;
  } catch {
    return null;
  }
}

export function uploadRecipeImageWithProgress(
  options: RecipeImageUploadOptions,
): RecipeImageUploadTask {
  const validationError = validateRecipeImageFile(options.file);
  if (validationError) {
    return {
      promise: Promise.reject(new TypeError(validationError)),
      cancel() {},
    };
  }

  const xhr = options.xhrFactory?.() ?? new XMLHttpRequest();
  const baseUrl = options.baseUrl ?? process.env.NEXT_PUBLIC_API_BASE_URL ?? "/api/v1";
  let settled = false;

  const promise = new Promise<RecipeApiResponse<RecipeImageDto>>((resolve, reject) => {
    xhr.open("POST", `${baseUrl}/recipes/${encodeURIComponent(options.recipeId)}/images`);
    xhr.withCredentials = true;
    xhr.setRequestHeader("Accept", "application/json");
    xhr.setRequestHeader("Authorization", `Bearer ${options.accessToken}`);
    xhr.setRequestHeader("If-Match", options.eTag);

    xhr.upload.onprogress = (event) => {
      if (!event.lengthComputable || event.total <= 0) return;
      const percentage = Math.min(100, Math.max(0, Math.round((event.loaded / event.total) * 100)));
      options.onProgress?.(percentage);
    };

    xhr.onload = () => {
      settled = true;
      if (xhr.status < 200 || xhr.status >= 300) {
        reject(new ApiError(
          xhr.status,
          parseJson<ApiProblemDetails>(xhr.responseText) ?? { title: "The API request failed." },
        ));
        return;
      }

      const data = parseJson<RecipeImageDto>(xhr.responseText);
      if (!data) {
        reject(new Error("The image API returned an invalid response."));
        return;
      }
      const eTag = xhr.getResponseHeader("ETag");
      if (!eTag?.trim()) {
        reject(new MissingRecipeEtagError());
        return;
      }
      options.onProgress?.(100);
      resolve({ data, eTag });
    };

    xhr.onerror = () => {
      settled = true;
      reject(new Error("The image upload could not reach the server."));
    };
    xhr.onabort = () => {
      settled = true;
      reject(new RecipeImageUploadCancelledError());
    };

    const formData = new FormData();
    formData.append("file", options.file, options.file.name);
    const altText = options.altText.trim();
    if (altText) formData.append("altText", altText);
    formData.append("isPrimary", String(options.isPrimary));
    xhr.send(formData);
  });

  return {
    promise,
    cancel() {
      if (!settled) xhr.abort();
    },
  };
}

type ImageMutationClient = Pick<RecipeClient, "deleteImage" | "detail" | "setPrimaryImage">;

export async function setPrimaryRecipeImage(
  client: ImageMutationClient,
  recipeId: string,
  imageId: string,
  eTag: string,
  images: readonly RecipeImageDto[],
): Promise<{ images: RecipeImageDto[]; eTag: string }> {
  const response = await client.setPrimaryImage(recipeId, imageId, eTag);
  if (!response.eTag) throw new MissingRecipeEtagError();
  return {
    eTag: response.eTag,
    images: images.map((image) => image.id === imageId
      ? { ...response.data, isPrimary: true }
      : { ...image, isPrimary: false }),
  };
}

export async function deleteRecipeImageAndRefresh(
  client: ImageMutationClient,
  recipeId: string,
  imageId: string,
  eTag: string,
): Promise<{ images: RecipeImageDto[]; eTag: string }> {
  await client.deleteImage(recipeId, imageId, eTag);
  try {
    const detail = await client.detail(recipeId);
    if (!detail.eTag) throw new MissingRecipeEtagError();
    return { images: detail.data.images, eTag: detail.eTag };
  } catch (error) {
    throw new RecipeImageDeletedRefreshError(imageId, error);
  }
}
