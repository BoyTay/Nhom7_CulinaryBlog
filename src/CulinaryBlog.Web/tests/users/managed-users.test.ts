import assert from "node:assert/strict";
import test from "node:test";
import {
  avatarTone,
  emptyUserFilters,
  explainUserError,
  lockNote,
  lockOptions,
  roleChips,
  statusChips,
  userActionCopy,
  userInitials,
  usersQueryString,
  visibleRange,
  type ManagedUser,
} from "../../lib/users/managed-users.ts";

const user: ManagedUser = {
  id: "abc",
  displayName: "Hoài Bo",
  email: "bo@example.com",
  avatarUrl: null,
  role: "Author",
  status: "Active",
  createdAt: "2026-01-15T00:00:00Z",
  recipeCount: 3,
  lockedUntil: null,
  lockDays: null,
};

const summary = { total: 12, authors: 10, admins: 2, locked: 3, joinedLast30Days: 2 };

test("the query string only carries the filters that are set", () => {
  assert.equal(usersQueryString(emptyUserFilters), "page=1&pageSize=8");
  assert.equal(
    usersQueryString({ search: "  thu hà ", role: "Admin", status: "Locked", page: 2 }),
    "page=2&pageSize=8&q=thu+h%C3%A0&role=Admin&status=Locked",
  );
});

test("the visible range is clamped to the total and empty when nothing matches", () => {
  assert.deepEqual(visibleRange(1, 8, 12), { from: 1, to: 8 });
  assert.deepEqual(visibleRange(2, 8, 12), { from: 9, to: 12 });
  assert.deepEqual(visibleRange(1, 8, 0), { from: 0, to: 0 });
});

test("the chips show the counts of all accounts", () => {
  assert.deepEqual(roleChips(summary).map((chip) => chip.count), [12, 10, 2]);
  assert.deepEqual(statusChips(summary).map((chip) => chip.count), [12, 9, 3]);
});

test("initials follow the board: the last two words, or two letters of a single word", () => {
  assert.equal(userInitials("Nguyễn Thu Hà"), "TH");
  assert.equal(userInitials("Hoài Bo"), "HB");
  assert.equal(userInitials("admin"), "AD");
  assert.equal(userInitials("   "), "?");
});

test("an account keeps the same avatar colour and it is one of the four", () => {
  assert.equal(avatarTone("a1b2"), avatarTone("a1b2"));
  assert.ok([0, 1, 2, 3].includes(avatarTone("recipe-test-author-1")));
});

test("each action names the account, and only locking is destructive", () => {
  assert.equal(userActionCopy({ kind: "lock", user, days: 7 }).title, "Khóa tài khoản của Hoài Bo?");
  assert.equal(userActionCopy({ kind: "lock", user, days: 7 }).destructive, true);
  assert.equal(userActionCopy({ kind: "unlock", user }).destructive, false);
  assert.equal(userActionCopy({ kind: "role", user, role: "Admin" }).done, "Đã đổi vai trò của Hoài Bo thành Admin");
});

test("known API errors get their own message and the rest fall back to a retry hint", () => {
  assert.match(explainUserError("USER_SELF_MANAGEMENT_FORBIDDEN"), /Không thể tự thay đổi/);
  assert.match(explainUserError("ADMIN_RIGHTS_REVOKED"), /không còn quyền quản trị/);
  assert.match(explainUserError("USER_NOT_FOUND"), /không còn tồn tại/);
  assert.match(explainUserError(undefined), /Vui lòng thử lại/);
});

test("the lock choices are the board's five, ending with the permanent one", () => {
  assert.deepEqual(lockOptions.map((option) => option.days), [1, 3, 7, 30, null]);
});

test("the toast says how long the account is locked", () => {
  assert.equal(userActionCopy({ kind: "lock", user, days: 7 }).done, "Đã khóa tài khoản của Hoài Bo trong 7 ngày");
  assert.equal(userActionCopy({ kind: "lock", user, days: null }).done, "Đã khóa tài khoản của Hoài Bo vĩnh viễn");
});

test("unlocking a timed lock mentions the date it would have ended", () => {
  const locked: ManagedUser = { ...user, status: "Locked", lockedUntil: "2026-11-09T03:00:00Z", lockDays: 7 };
  assert.match(userActionCopy({ kind: "unlock", user: locked }).description, /hết hạn khóa \(09\/11\/2026\)/);
  assert.equal(userActionCopy({ kind: "unlock", user: { ...locked, lockedUntil: null, lockDays: null } }).description, "Họ có thể đăng nhập lại ngay sau khi bạn xác nhận.");
});

test("the row note shows the end date, the permanent wording, or nothing", () => {
  assert.equal(lockNote({ status: "Locked", lockedUntil: "2026-11-09T03:00:00Z", lockDays: 7 }), "Khóa đến 09/11/2026 (7 ngày)");
  assert.equal(lockNote({ status: "Locked", lockedUntil: null, lockDays: null }), "Khóa vĩnh viễn, cho đến khi mở khóa");
  assert.equal(lockNote({ status: "Active", lockedUntil: null, lockDays: null }), "");
});
