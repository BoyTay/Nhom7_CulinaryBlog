// Request/response shapes for the password and profile endpoints added to the Auth API in Lab 7.
// They live with the Auth module instead of the shared lib/api/contracts.ts, which Hiếu owns.

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}

export interface MessageResponse {
  message: string;
}
