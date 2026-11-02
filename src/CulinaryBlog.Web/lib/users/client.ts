import { apiFetch } from "@/lib/api/client";
import { authStore } from "@/lib/auth/auth-store";
import { usersQueryString, type ManagedUserList, type UserAction, type UserFilters } from "./managed-users";

const authorization = () => ({ Authorization: `Bearer ${authStore.getAccessToken() ?? ""}` });

function put(path: string, body: unknown): Promise<void> {
  return apiFetch<void>(path, {
    method: "PUT",
    headers: { ...authorization(), "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
}

export function listManagedUsers(filters: UserFilters): Promise<ManagedUserList> {
  return apiFetch<ManagedUserList>(`/users?${usersQueryString(filters)}`, { headers: authorization() });
}

export function runUserAction(action: UserAction): Promise<void> {
  const path = `/users/${encodeURIComponent(action.user.id)}`;
  switch (action.kind) {
    case "role":
      return put(`${path}/role`, { role: action.role });
    case "lock":
      return put(`${path}/lock`, { locked: true, days: action.days });
    case "unlock":
      return put(`${path}/lock`, { locked: false });
  }
}
