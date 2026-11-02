"use client";

import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Image from "next/image";
import { useEffect, useState } from "react";
import { ConfirmDialog, ConfirmDialogIcon } from "@/components/dashboard/confirm-dialog";
import { Alert, Pagination, Skeleton, SkeletonGroup, useToast } from "@/components/shared";
import { ApiError } from "@/lib/api/client";
import { useAuth } from "@/lib/auth/use-auth";
import { queryKeys } from "@/lib/query/keys";
import { formatDateVi } from "@/lib/ui/format";
import { listManagedUsers, runUserAction } from "@/lib/users/client";
import {
  DEFAULT_LOCK_DAYS,
  avatarTone,
  emptyUserFilters,
  explainUserError,
  lockNote,
  lockOptions,
  roleChips,
  roleDetails,
  statusChips,
  statusLabels,
  userActionCopy,
  userInitials,
  visibleRange,
  type ManagedUser,
  type ManagedUserSummary,
  type UserAction,
  type UserFilters,
} from "@/lib/users/managed-users";
import "./users.css";

const SEARCH_DELAY_MS = 300;

const dialogIcons: Record<UserAction["kind"], { path: string; tone: "gold" | "danger" | "leaf" }> = {
  role: { path: "M12 3l8 3v6c0 4.5-3.2 8-8 9-4.8-1-8-4.5-8-9V6z", tone: "gold" },
  lock: { path: "M6 11h12v9H6zM8 11V8a4 4 0 0 1 8 0v3", tone: "danger" },
  unlock: { path: "M6 11h12v9H6zM8 11V8a4 4 0 0 1 7.5-2", tone: "leaf" },
};

function Stats({ summary }: { summary: ManagedUserSummary }) {
  const stats = [
    { label: "Tổng tài khoản", value: summary.total, tone: "sand" },
    { label: "Tác giả", value: summary.authors, tone: "leaf" },
    { label: "Admin", value: summary.admins, tone: "gold" },
    { label: "Bị khóa", value: summary.locked, tone: "rose" },
    { label: "Tham gia 30 ngày qua", value: summary.joinedLast30Days, tone: "gold" },
  ];
  return (
    <section className="users-stats" aria-label="Tổng quan tài khoản">
      {stats.map((stat) => (
        <div key={stat.label} className={`rise users-stat users-stat-${stat.tone}`}>
          <span>{stat.label}</span>
          <strong>{stat.value}</strong>
        </div>
      ))}
    </section>
  );
}

function FilterChips<Value extends string>({
  labelId,
  label,
  chips,
  selected,
  onSelect,
}: {
  labelId: string;
  label: string;
  chips: readonly { value: Value | null; label: string; count: number }[];
  selected: Value | null;
  onSelect: (value: Value | null) => void;
}) {
  return (
    <div className="users-filter" role="group" aria-labelledby={labelId}>
      <span id={labelId} className="users-label">{label}</span>
      <div className="users-chips">
        {chips.map((chip) => (
          <button key={chip.label} type="button" className="users-chip" aria-pressed={chip.value === selected} onClick={() => onSelect(chip.value)}>
            {chip.label} <span>{chip.count}</span>
          </button>
        ))}
      </div>
    </div>
  );
}

function UserRow({
  user,
  isSelf,
  busy,
  onAction,
}: {
  user: ManagedUser;
  isSelf: boolean;
  busy: boolean;
  onAction: (action: UserAction) => void;
}) {
  const name = user.displayName;
  const locked = user.status === "Locked";
  const note = lockNote(user);
  return (
    <li className="users-row">
      <div className="users-row-who">
        <span className={`users-avatar users-avatar-${avatarTone(user.id)}`} aria-hidden="true">
          {user.avatarUrl ? <Image src={user.avatarUrl} alt="" width={52} height={52} unoptimized /> : userInitials(name)}
        </span>
        <div className="users-row-text">
          <strong>{name}{isSelf && <span> (bạn)</span>}</strong>
          <span className="users-hint users-email">{user.email}</span>
          <span className="users-hint">Tham gia {formatDateVi(user.createdAt)} · {user.recipeCount} công thức</span>
          {note && <span className="users-lock-note">{note}</span>}
        </div>
      </div>
      <span className="users-cell-role"><span className={`users-badge users-role-${user.role.toLowerCase()}`}>{roleDetails[user.role].label}</span></span>
      <span className="users-cell-status">
        <span className={`users-badge users-status-${user.status.toLowerCase()}`}><span className="users-dot" aria-hidden="true" />{statusLabels[user.status]}</span>
      </span>
      <div className="users-row-actions">
        {isSelf ? (
          <span className="users-hint">Không thể tự khóa tài khoản đang đăng nhập</span>
        ) : (
          <>
            <button type="button" className="btn btn-outline" aria-label={`Đổi vai trò của ${name}`} disabled={busy} onClick={() => onAction({ kind: "role", user, role: user.role })}>Vai trò</button>
            <button type="button" className="btn btn-soft" aria-label={`${locked ? "Mở khóa" : "Khóa"} ${name}`} disabled={busy} onClick={() => onAction(locked ? { kind: "unlock", user } : { kind: "lock", user, days: DEFAULT_LOCK_DAYS })}>{locked ? "Mở khóa" : "Khóa"}</button>
          </>
        )}
      </div>
    </li>
  );
}

function Choices<Value extends string | number | null>({
  label,
  options,
  selected,
  onSelect,
}: {
  label: string;
  options: readonly { value: Value; label: string; description: string }[];
  selected: Value;
  onSelect: (value: Value) => void;
}) {
  return (
    <div className="users-choices" role="radiogroup" aria-label={label}>
      {options.map((option) => (
        <label key={String(option.value)} className="users-choice">
          <input type="radio" name="managed-user-choice" checked={selected === option.value} onChange={() => onSelect(option.value)} />
          <span className="users-choice-ring" aria-hidden="true" />
          <span className="users-choice-text">
            <strong>{option.label}</strong>
            <span className="users-hint">{option.description}</span>
          </span>
        </label>
      ))}
    </div>
  );
}

function ListSkeleton() {
  return (
    <SkeletonGroup label="Đang tải danh sách tài khoản" className="users-list">
      {[0, 1, 2, 3].map((row) => (
        <div className="users-row" key={row}>
          <div className="users-row-who">
            <Skeleton className="ui-skeleton-pill" width="52px" height="52px" />
            <div className="users-row-text users-row-text-loading">
              <Skeleton width="55%" height="18px" />
              <Skeleton width="75%" height="13px" />
            </div>
          </div>
          <Skeleton className="ui-skeleton-pill" width="96px" height="28px" />
        </div>
      ))}
    </SkeletonGroup>
  );
}

export default function UserManager() {
  const { user: currentUser } = useAuth();
  const toast = useToast();
  const queryClient = useQueryClient();
  const [searchInput, setSearchInput] = useState("");
  const [filters, setFilters] = useState<UserFilters>(emptyUserFilters);
  const [pending, setPending] = useState<UserAction | null>(null);

  useEffect(() => {
    const search = searchInput.trim();
    if (search === filters.search) return;
    const timer = setTimeout(() => setFilters((current) => ({ ...current, search, page: 1 })), SEARCH_DELAY_MS);
    return () => clearTimeout(timer);
  }, [searchInput, filters.search]);

  const list = useQuery({
    queryKey: queryKeys.users.list(filters),
    queryFn: () => listManagedUsers(filters),
    placeholderData: keepPreviousData,
  });

  // Unlocking the last row while filtering by "Bị khóa" would otherwise leave an empty page behind.
  const totalPages = list.data?.users.totalPages ?? 0;
  useEffect(() => {
    if (!list.isPlaceholderData && totalPages > 0 && filters.page > totalPages) {
      setFilters((current) => ({ ...current, page: totalPages }));
    }
  }, [filters.page, list.isPlaceholderData, totalPages]);

  const mutation = useMutation({
    mutationFn: runUserAction,
    onSuccess: (_result, action) => {
      toast.success(userActionCopy(action).done);
    },
    onError: (error) => {
      toast.error(explainUserError(error instanceof ApiError ? error.problem.type : undefined));
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: queryKeys.users.all }),
  });

  function confirmPending() {
    if (!pending) return;
    setPending(null);
    if (pending.kind === "role" && pending.role === pending.user.role) return;
    mutation.mutate(pending);
  }

  function resetFilters() {
    setSearchInput("");
    setFilters(emptyUserFilters);
  }

  const data = list.data;
  const copy = pending && userActionCopy(pending);
  const icon = pending && dialogIcons[pending.kind];
  const range = data ? visibleRange(data.users.page, data.users.pageSize, data.users.totalCount) : null;

  return (
    <div className="users">
      <div className="users-heading">
        <h1 className="rise">Quản lý tài khoản</h1>
        <p>Danh sách tài khoản Admin và Tác giả: đổi vai trò, khóa theo số ngày hoặc mở khóa tài khoản vi phạm.</p>
      </div>

      {data && <Stats summary={data.summary} />}

      <section className="users-card" aria-label="Tìm kiếm và bộ lọc">
        <div className="users-search">
          <label htmlFor="user-search" className="users-label">Tìm tài khoản</label>
          <div className="users-search-box">
            <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><circle cx="11" cy="11" r="7" /><path d="M20 20l-4-4" /></svg>
            <input id="user-search" type="search" placeholder="Nhập tên hoặc email" autoComplete="off" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} />
          </div>
        </div>
        {data && (
          <div className="users-filters">
            <FilterChips labelId="user-role-filter" label="Vai trò" chips={roleChips(data.summary)} selected={filters.role} onSelect={(role) => setFilters((current) => ({ ...current, role, page: 1 }))} />
            <FilterChips labelId="user-status-filter" label="Trạng thái" chips={statusChips(data.summary)} selected={filters.status} onSelect={(status) => setFilters((current) => ({ ...current, status, page: 1 }))} />
          </div>
        )}
      </section>

      <section className="users-list-section" aria-labelledby="users-list-title">
        <h2 id="users-list-title" aria-live="polite">
          Danh sách tài khoản {data && <small>· {data.users.totalCount}</small>}
        </h2>

        {list.isPending && <ListSkeleton />}

        {list.isError && !data && (
          <Alert variant="error" title="Chưa thể tải danh sách tài khoản" action={<button type="button" onClick={() => void list.refetch()}>Thử lại</button>}>
            Vui lòng kiểm tra kết nối rồi thử lại.
          </Alert>
        )}

        {data && data.users.totalCount === 0 && (
          <div className="users-empty">
            <span aria-hidden="true"><svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.9" strokeLinecap="round"><circle cx="11" cy="11" r="7" /><path d="M20 20l-4-4" /></svg></span>
            <strong>Không tìm thấy tài khoản nào</strong>
            <p>Thử đổi từ khóa hoặc bỏ bớt bộ lọc để xem nhiều kết quả hơn.</p>
            <button type="button" className="btn btn-outline" onClick={resetFilters}>Xóa bộ lọc</button>
          </div>
        )}

        {data && data.users.totalCount > 0 && (
          <>
            <ul className="users-list">
              {data.users.items.map((user) => (
                <UserRow key={user.id} user={user} isSelf={user.id === currentUser?.id} busy={mutation.isPending} onAction={setPending} />
              ))}
            </ul>
            <div className="users-pager">
              <span className="users-hint">Hiển thị {range?.from}–{range?.to} trên {data.users.totalCount} tài khoản</span>
              <Pagination
                className="users-pagination"
                page={data.users.page}
                totalPages={Math.max(data.users.totalPages, 1)}
                previousLabel="Trước"
                nextLabel="Sau"
                disabled={list.isPlaceholderData}
                onPageChange={(page) => setFilters((current) => ({ ...current, page }))}
              />
            </div>
          </>
        )}
      </section>

      <ConfirmDialog
        className="users-dialog"
        open={pending !== null}
        title={copy?.title ?? ""}
        description={copy?.description ?? ""}
        confirmLabel={copy?.confirmLabel ?? ""}
        destructive={copy?.destructive}
        onConfirm={confirmPending}
        onCancel={() => setPending(null)}
        icon={icon && <ConfirmDialogIcon path={icon.path} tone={icon.tone} />}
      >
        {pending?.kind === "role" && (
          <Choices
            label="Chọn vai trò"
            options={(["Author", "Admin"] as const).map((role) => ({ value: role, label: roleDetails[role].label, description: roleDetails[role].description }))}
            selected={pending.role}
            onSelect={(role) => setPending({ ...pending, role })}
          />
        )}
        {pending?.kind === "lock" && (
          <Choices
            label="Chọn thời gian khóa"
            options={lockOptions.map((option) => ({ value: option.days, label: option.label, description: option.description }))}
            selected={pending.days}
            onSelect={(days) => setPending({ ...pending, days })}
          />
        )}
      </ConfirmDialog>
    </div>
  );
}
