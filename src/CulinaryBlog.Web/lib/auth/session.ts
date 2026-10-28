import type { ApiProblemDetails } from "../api/contracts.ts";

/** Renew this long before the access token expires so requests never race the expiry. */
export const REFRESH_LEAD_SECONDS = 60;
const MIN_REFRESH_DELAY_MS = 5_000;

/** Milliseconds to wait before silently renewing an access token that lives `expiresInSeconds`. */
export function refreshDelayMs(expiresInSeconds: number): number {
  return Math.max(MIN_REFRESH_DELAY_MS, (expiresInSeconds - REFRESH_LEAD_SECONDS) * 1000);
}

/** True when an access token issued with `expiresAt` (epoch ms) is inside the renewal window. */
export function needsRefresh(expiresAt: number, now: number = Date.now()): boolean {
  return now >= expiresAt - REFRESH_LEAD_SECONDS * 1000;
}

/**
 * The API puts its error code in ProblemDetails.type (ACCOUNT_LOCKED for password login, refresh and
 * Google). errorCode/ACCOUNT_LOCKED_OUT are accepted only for responses from older API builds.
 */
export function isAccountLocked(problem: ApiProblemDetails): boolean {
  return [problem.type, problem.errorCode].some(
    (code) => code === "ACCOUNT_LOCKED" || code === "ACCOUNT_LOCKED_OUT",
  );
}
