import type { PagedResult } from "../api/contracts.ts";
import { formatDateVi } from "../ui/format.ts";

export type ManagedUserRole = "Admin" | "Author";
export type ManagedUserStatus = "Active" | "Locked";

export interface ManagedUser {
  id: string;
  displayName: string;
  email: string;
  avatarUrl: string | null;
  role: ManagedUserRole;
  status: ManagedUserStatus;
  createdAt: string;
  recipeCount: number;
  lockedUntil: string | null;
  lockDays: number | null;
}

export interface ManagedUserSummary {
  total: number;
  authors: number;
  admins: number;
  locked: number;
  joinedLast30Days: number;
}

export interface ManagedUserList {
  summary: ManagedUserSummary;
  users: PagedResult<ManagedUser>;
}

export interface UserFilters {
  search: string;
  role: ManagedUserRole | null;
  status: ManagedUserStatus | null;
  page: number;
}

export const USERS_PAGE_SIZE = 8;

export const emptyUserFilters: UserFilters = { search: "", role: null, status: null, page: 1 };

export const roleDetails: Record<ManagedUserRole, { label: string; description: string }> = {
  Author: { label: "Tác giả", description: "Viết, xuất bản và quản lý công thức của mình." },
  Admin: { label: "Admin", description: "Toàn quyền: quản lý danh mục và tài khoản người khác." },
};

/** `days` is null for a lock that lasts until an admin lifts it. */
export const lockOptions: readonly { days: number | null; label: string; description: string }[] = [
  { days: 1, label: "1 ngày", description: "Tự mở khóa sau 1 ngày." },
  { days: 3, label: "3 ngày", description: "Tự mở khóa sau 3 ngày." },
  { days: 7, label: "7 ngày", description: "Tự mở khóa sau 1 tuần." },
  { days: 30, label: "30 ngày", description: "Tự mở khóa sau 30 ngày." },
  { days: null, label: "Vĩnh viễn", description: "Giữ khóa cho đến khi bạn mở khóa thủ công." },
];

export const DEFAULT_LOCK_DAYS = 7;

export const statusLabels: Record<ManagedUserStatus, string> = { Active: "Hoạt động", Locked: "Bị khóa" };

export function usersQueryString(filters: UserFilters): string {
  const query = new URLSearchParams({ page: String(filters.page), pageSize: String(USERS_PAGE_SIZE) });
  const search = filters.search.trim();
  if (search) query.set("q", search);
  if (filters.role) query.set("role", filters.role);
  if (filters.status) query.set("status", filters.status);
  return query.toString();
}

/** The "Hiển thị 1–8" part of the pagination line; both ends are 0 when nothing matches. */
export function visibleRange(page: number, pageSize: number, totalCount: number): { from: number; to: number } {
  if (totalCount === 0) return { from: 0, to: 0 };
  return { from: (page - 1) * pageSize + 1, to: Math.min(page * pageSize, totalCount) };
}

export function roleChips(summary: ManagedUserSummary) {
  return [
    { value: null, label: "Tất cả", count: summary.total },
    { value: "Author", label: roleDetails.Author.label, count: summary.authors },
    { value: "Admin", label: roleDetails.Admin.label, count: summary.admins },
  ] as const;
}

export function statusChips(summary: ManagedUserSummary) {
  return [
    { value: null, label: "Tất cả", count: summary.total },
    { value: "Active", label: statusLabels.Active, count: summary.total - summary.locked },
    { value: "Locked", label: statusLabels.Locked, count: summary.locked },
  ] as const;
}

/** The last two words of the name, as on the Users board: "Nguyễn Thu Hà" becomes "TH". */
export function userInitials(name: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean);
  if (words.length === 0) return "?";
  const letters = words.length > 1
    ? [...words[words.length - 2]][0] + [...words[words.length - 1]][0]
    : [...words[0]].slice(0, 2).join("");
  return letters.toUpperCase();
}

/** One of the four avatar colours, stable for an account because ids are not sequential. */
export function avatarTone(userId: string): number {
  return [...userId].reduce((total, character) => total + character.charCodeAt(0), 0) % 4;
}

/** The red line on a locked account's row; empty for an account that is not locked. */
export function lockNote(user: Pick<ManagedUser, "status" | "lockedUntil" | "lockDays">): string {
  if (user.status !== "Locked") return "";
  return user.lockedUntil && user.lockDays
    ? `Khóa đến ${formatDateVi(user.lockedUntil)} (${user.lockDays} ngày)`
    : "Khóa vĩnh viễn, cho đến khi mở khóa";
}

export type UserAction =
  | { kind: "role"; user: ManagedUser; role: ManagedUserRole }
  | { kind: "lock"; user: ManagedUser; days: number | null }
  | { kind: "unlock"; user: ManagedUser };

export interface UserActionCopy {
  title: string;
  description: string;
  confirmLabel: string;
  done: string;
  destructive: boolean;
}

export function userActionCopy(action: UserAction): UserActionCopy {
  const name = action.user.displayName;
  switch (action.kind) {
    case "role":
      return {
        title: `Đổi vai trò của ${name}`,
        description: `Chọn quyền hạn mới cho ${action.user.email}. Thay đổi có hiệu lực ở lần tải trang tiếp theo của họ.`,
        confirmLabel: "Lưu vai trò",
        done: `Đã đổi vai trò của ${name} thành ${roleDetails[action.role].label}`,
        destructive: false,
      };
    case "lock":
      return {
        title: `Khóa tài khoản của ${name}?`,
        description: "Họ sẽ không đăng nhập được trong thời gian khóa. Công thức đã xuất bản vẫn hiển thị với người đọc. Tài khoản không bị xóa và có thể mở khóa bất cứ lúc nào.",
        confirmLabel: "Khóa tài khoản",
        done: `Đã khóa tài khoản của ${name} ${action.days ? `trong ${action.days} ngày` : "vĩnh viễn"}`,
        destructive: true,
      };
    case "unlock":
      return {
        title: `Mở khóa tài khoản của ${name}?`,
        description: action.user.lockedUntil
          ? `Họ có thể đăng nhập lại ngay sau khi bạn xác nhận, không cần chờ đến hết hạn khóa (${formatDateVi(action.user.lockedUntil)}).`
          : "Họ có thể đăng nhập lại ngay sau khi bạn xác nhận.",
        confirmLabel: "Mở khóa",
        done: `Đã mở khóa tài khoản của ${name}`,
        destructive: false,
      };
  }
}

export function explainUserError(code: string | undefined): string {
  switch (code) {
    case "USER_SELF_MANAGEMENT_FORBIDDEN":
      return "Không thể tự thay đổi tài khoản đang đăng nhập.";
    case "ADMIN_RIGHTS_REVOKED":
      return "Tài khoản của bạn không còn quyền quản trị. Hãy tải lại trang.";
    case "USER_NOT_FOUND":
      return "Tài khoản này không còn tồn tại. Danh sách đã được tải lại.";
    default:
      return "Chưa thực hiện được thao tác. Vui lòng thử lại.";
  }
}
