"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { ApiError } from "@/lib/api/client";
import type {
  AuthResponseDto,
  AuthUserDto,
  LoginRequest,
  RegisterRequest,
  UpdateProfileRequest,
} from "@/lib/api/contracts";
import type { ChangePasswordRequest } from "@/lib/auth/password-contracts";
import {
  changePasswordApi,
  googleLoginApi,
  loginApi,
  logoutApi,
  refreshTokenApi,
  registerApi,
  updateProfileApi,
  uploadAvatarApi,
} from "@/lib/auth/auth-client";
import { authStore } from "@/lib/auth/auth-store";
import { needsRefresh, refreshDelayMs } from "@/lib/auth/session";

export interface AuthContextValue {
  user: AuthUserDto | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (req: LoginRequest) => Promise<AuthUserDto>;
  loginWithGoogle: (idToken: string) => Promise<AuthUserDto>;
  register: (req: RegisterRequest) => Promise<AuthUserDto>;
  logout: () => Promise<void>;
  updateProfile: (req: UpdateProfileRequest) => Promise<AuthUserDto>;
  uploadAvatar: (file: File) => Promise<AuthUserDto>;
  /**
   * Changes the password. The server revokes every session and deletes the refresh cookie,
   * so the caller must leave with a full page load (which re-runs the silent refresh and signs out).
   */
  changePassword: (req: ChangePasswordRequest) => Promise<string>;
  refreshSession: () => Promise<boolean>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUserDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const refreshTimer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);
  const accessTokenExpiresAt = useRef(0);
  const refreshSessionRef = useRef<() => Promise<boolean>>(async () => false);

  const endSession = useCallback(() => {
    clearTimeout(refreshTimer.current);
    accessTokenExpiresAt.current = 0;
    authStore.clearAccessToken();
    setUser(null);
  }, []);

  // Access tokens live 15 minutes; renew them shortly before expiry so Profile and Recipe requests
  // made with authStore's token never start failing with 401 while the user is still active.
  const startSession = useCallback((response: AuthResponseDto) => {
    authStore.setAccessToken(response.accessToken);
    accessTokenExpiresAt.current = Date.now() + response.expiresIn * 1000;
    clearTimeout(refreshTimer.current);
    refreshTimer.current = setTimeout(() => {
      void refreshSessionRef.current();
    }, refreshDelayMs(response.expiresIn));
    setUser(response.user);
    return response.user;
  }, []);

  const refreshSession = useCallback(async (): Promise<boolean> => {
    try {
      startSession(await refreshTokenApi());
      return true;
    } catch {
      endSession();
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [startSession, endSession]);
  refreshSessionRef.current = refreshSession;

  useEffect(() => {
    // Attempt silent refresh on initial app load via HttpOnly cookie (ADR-0003)
    void refreshSession();
    return () => clearTimeout(refreshTimer.current);
  }, [refreshSession]);

  useEffect(() => {
    // Timers are throttled or paused in background tabs and during sleep; catch up when the tab returns.
    const onVisible = () => {
      if (document.visibilityState === "visible" && accessTokenExpiresAt.current > 0
        && needsRefresh(accessTokenExpiresAt.current)) {
        void refreshSessionRef.current();
      }
    };
    document.addEventListener("visibilitychange", onVisible);
    return () => document.removeEventListener("visibilitychange", onVisible);
  }, []);

  const login = useCallback(
    async (req: LoginRequest): Promise<AuthUserDto> => startSession(await loginApi(req)),
    [startSession],
  );

  const loginWithGoogle = useCallback(
    async (idToken: string): Promise<AuthUserDto> => startSession(await googleLoginApi({ idToken })),
    [startSession],
  );

  const register = useCallback(
    async (req: RegisterRequest): Promise<AuthUserDto> => startSession(await registerApi(req)),
    [startSession],
  );

  const logout = useCallback(async (): Promise<void> => {
    try {
      await logoutApi();
    } catch (error) {
      // /logout requires a valid access token. With an expired one it answers 401 and the refresh cookie
      // survives, which would silently sign the user back in on the next page load: renew once and retry.
      if (error instanceof ApiError && error.status === 401) {
        try {
          authStore.setAccessToken((await refreshTokenApi()).accessToken);
          await logoutApi();
        } catch (retryError) {
          console.warn("Logout retry failed; the session was already gone:", retryError);
        }
      } else {
        console.warn("Logout request failed:", error);
      }
    } finally {
      endSession();
    }
  }, [endSession]);

  const updateProfile = useCallback(async (req: UpdateProfileRequest): Promise<AuthUserDto> => {
    const updatedUser = await updateProfileApi(req);
    setUser(updatedUser);
    return updatedUser;
  }, []);

  const uploadAvatar = useCallback(async (file: File): Promise<AuthUserDto> => {
    const updatedUser = await uploadAvatarApi(file);
    setUser(updatedUser);
    return updatedUser;
  }, []);

  const changePassword = useCallback(async (req: ChangePasswordRequest): Promise<string> => {
    const response = await changePasswordApi(req);
    // Only the token is dropped here: clearing `user` would make ProtectedRoute redirect
    // before the caller can navigate to its own confirmation page. The scheduled renewal is
    // cancelled too, since the server already revoked the refresh cookie it would use.
    clearTimeout(refreshTimer.current);
    accessTokenExpiresAt.current = 0;
    authStore.clearAccessToken();
    return response.message;
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: user !== null,
      isLoading,
      login,
      loginWithGoogle,
      register,
      logout,
      updateProfile,
      uploadAvatar,
      changePassword,
      refreshSession,
    }),
    [user, isLoading, login, loginWithGoogle, register, logout, updateProfile, uploadAvatar, changePassword, refreshSession],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}

