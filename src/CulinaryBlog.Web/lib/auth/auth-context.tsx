"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import type {
  AuthUserDto,
  LoginRequest,
  RegisterRequest,
  UpdateProfileRequest,
} from "@/lib/api/contracts";
import {
  googleLoginApi,
  loginApi,
  logoutApi,
  refreshTokenApi,
  registerApi,
  updateProfileApi,
} from "@/lib/auth/auth-client";
import { authStore } from "@/lib/auth/auth-store";

export interface AuthContextValue {
  user: AuthUserDto | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (req: LoginRequest) => Promise<AuthUserDto>;
  loginWithGoogle: (idToken: string) => Promise<AuthUserDto>;
  register: (req: RegisterRequest) => Promise<AuthUserDto>;
  logout: () => Promise<void>;
  updateProfile: (req: UpdateProfileRequest) => Promise<AuthUserDto>;
  refreshSession: () => Promise<boolean>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUserDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const refreshSession = useCallback(async (): Promise<boolean> => {
    try {
      const response = await refreshTokenApi();
      authStore.setAccessToken(response.accessToken);
      setUser(response.user);
      return true;
    } catch {
      authStore.clearAccessToken();
      setUser(null);
      return false;
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    // Attempt silent refresh on initial app load via HttpOnly cookie (ADR-0003)
    void refreshSession();
  }, [refreshSession]);

  const login = useCallback(async (req: LoginRequest): Promise<AuthUserDto> => {
    const response = await loginApi(req);
    authStore.setAccessToken(response.accessToken);
    setUser(response.user);
    return response.user;
  }, []);

  const loginWithGoogle = useCallback(async (idToken: string): Promise<AuthUserDto> => {
    const response = await googleLoginApi({ idToken });
    authStore.setAccessToken(response.accessToken);
    setUser(response.user);
    return response.user;
  }, []);

  const register = useCallback(async (req: RegisterRequest): Promise<AuthUserDto> => {
    const response = await registerApi(req);
    authStore.setAccessToken(response.accessToken);
    setUser(response.user);
    return response.user;
  }, []);

  const logout = useCallback(async (): Promise<void> => {
    try {
      await logoutApi();
    } catch (error) {
      console.warn("Logout request failed or user was already unauthenticated:", error);
    } finally {
      authStore.clearAccessToken();
      setUser(null);
    }
  }, []);

  const updateProfile = useCallback(async (req: UpdateProfileRequest): Promise<AuthUserDto> => {
    const updatedUser = await updateProfileApi(req);
    setUser(updatedUser);
    return updatedUser;
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
      refreshSession,
    }),
    [user, isLoading, login, loginWithGoogle, register, logout, updateProfile, refreshSession],
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

