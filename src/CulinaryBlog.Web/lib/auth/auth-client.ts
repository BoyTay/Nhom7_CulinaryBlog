import { ApiError } from "@/lib/api/client";
import type {
  ApiProblemDetails,
  AuthResponseDto,
  AuthUserDto,
  GoogleLoginRequest,
  LoginRequest,
  RegisterRequest,
  UpdateProfileRequest,
} from "@/lib/api/contracts";
import { authStore } from "@/lib/auth/auth-store";

const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? "/api/v1";

/**
 * Executes a fetch request to the Auth API with standard headers,
 * in-memory bearer token authorization, and credential forwarding for HttpOnly cookies (ADR-0003).
 */
export async function authFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const token = authStore.getAccessToken();
  const headers = new Headers(init?.headers);

  if (!headers.has("Accept")) {
    headers.set("Accept", "application/json");
  }

  if (init?.body && !(init.body instanceof FormData) && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  if (token && !headers.has("Authorization")) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers,
    credentials: "include",
    cache: "no-store",
  });

  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ApiProblemDetails;
    throw new ApiError(response.status, problem);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export async function loginApi(request: LoginRequest): Promise<AuthResponseDto> {
  return authFetch<AuthResponseDto>("/auth/login", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export async function registerApi(request: RegisterRequest): Promise<AuthResponseDto> {
  return authFetch<AuthResponseDto>("/auth/register", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export async function googleLoginApi(request: GoogleLoginRequest): Promise<AuthResponseDto> {
  return authFetch<AuthResponseDto>("/auth/google", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export async function refreshTokenApi(): Promise<AuthResponseDto> {
  return authFetch<AuthResponseDto>("/auth/refresh", {
    method: "POST",
  });
}

export async function logoutApi(): Promise<void> {
  return authFetch<void>("/auth/logout", {
    method: "POST",
  });
}

export async function getMeApi(): Promise<AuthUserDto> {
  return authFetch<AuthUserDto>("/auth/me", {
    method: "GET",
  });
}

export async function updateProfileApi(request: UpdateProfileRequest): Promise<AuthUserDto> {
  return authFetch<AuthUserDto>("/auth/me", {
    method: "PATCH",
    body: JSON.stringify(request),
  });
}
