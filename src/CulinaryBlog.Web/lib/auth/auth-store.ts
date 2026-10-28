/**
 * In-Memory Access Token Store (ADR-0003)
 *
 * Security Guarantee:
 * Access tokens are stored ONLY in memory closure variables.
 * They are NEVER written to localStorage, sessionStorage, or client-accessible cookies,
 * effectively preventing token theft via XSS vulnerabilities.
 */

type TokenChangeListener = (token: string | null) => void;

let memoryAccessToken: string | null = null;
let intentionalSignOut = false;
const listeners = new Set<TokenChangeListener>();

export const authStore = {
  getAccessToken(): string | null {
    return memoryAccessToken;
  },

  setAccessToken(token: string | null): void {
    memoryAccessToken = token;
    listeners.forEach((listener) => {
      try {
        listener(token);
      } catch (error) {
        console.error("Error in token change listener:", error);
      }
    });
  },

  clearAccessToken(): void {
    this.setAccessToken(null);
  },

  /** Remembers that the user chose to sign out, so a protected page does not send them back to /login?returnUrl=. */
  markIntentionalSignOut(): void {
    intentionalSignOut = true;
  },

  consumeIntentionalSignOut(): boolean {
    const wasIntentional = intentionalSignOut;
    intentionalSignOut = false;
    return wasIntentional;
  },

  onTokenChange(listener: TokenChangeListener): () => void {
    listeners.add(listener);
    return () => {
      listeners.delete(listener);
    };
  },
};

