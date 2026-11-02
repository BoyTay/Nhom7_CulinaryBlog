"use client";

import { useMutation, useQueries, useQuery, useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { useState } from "react";
import { Alert, Skeleton, SkeletonGroup, useToast } from "@/components/shared";
import { ApiError } from "@/lib/api/client";
import {
  actionConfirmation,
  actionIcons,
  actionLabels,
  availableActions,
  explainLifecycleError,
  filterByStatus,
  missingParts,
  runRecipeAction,
  summarizeMyRecipes,
  type RecipeAction,
} from "@/lib/dashboard/my-recipes";
import { RecipeImage } from "@/components/recipes/recipe-image";
import { recipeClient } from "@/lib/recipes/client";
import type { RecipeListItemDto } from "@/lib/recipes/contracts";
import { recipeDetailHref, routes } from "@/lib/navigation/routes";
import { relativeTimeVi } from "@/lib/ui/format";
import { ConfirmDialog, ConfirmDialogIcon } from "./confirm-dialog";
import { RecipeStatusBadge } from "./recipe-status-badge";
import { StatusTabs, useStatusTab } from "./status-tabs";

export const myRecipesQueryKey = ["recipes", "mine"] as const;

export function useMyRecipes() {
  return useQuery({
    queryKey: myRecipesQueryKey,
    queryFn: async () => (await recipeClient.listMine()).data,
  });
}

/**
 * What each draft still lacks, by recipe id. The list endpoint carries no ingredient or step counts, so this reads
 * the detail of drafts only. An id is absent until its answer arrives.
 */
export function useDraftGaps(recipes: readonly RecipeListItemDto[]): ReadonlyMap<string, string | null> {
  const drafts = recipes.filter((recipe) => recipe.status === "Draft");
  const results = useQueries({
    queries: drafts.map((recipe) => ({
      queryKey: [...myRecipesQueryKey, "gaps", recipe.id],
      queryFn: async () => missingParts((await recipeClient.detail(recipe.id)).data),
      staleTime: 0,
    })),
  });

  const gaps = new Map<string, string | null>();
  drafts.forEach((recipe, index) => {
    const result = results[index];
    if (result.isSuccess) gaps.set(recipe.id, result.data);
  });
  return gaps;
}

interface PendingAction {
  recipe: RecipeListItemDto;
  action: RecipeAction;
}

function ActionIcon({ path }: { path: string }) {
  return (
    <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.9" strokeLinecap="round" strokeLinejoin="round"><path d={path} /></svg>
  );
}

export function MyRecipesTable() {
  const recipes = useMyRecipes();
  const gaps = useDraftGaps(recipes.data ?? []);
  const tab = useStatusTab();
  const queryClient = useQueryClient();
  const toast = useToast();
  const [pending, setPending] = useState<PendingAction | null>(null);

  const mutation = useMutation({
    mutationFn: ({ recipe, action }: PendingAction) => runRecipeAction(recipeClient, recipe.id, action),
    onSuccess: (_result, { action }) => {
      toast.success(actionLabels[action].done);
      void queryClient.invalidateQueries({ queryKey: myRecipesQueryKey });
    },
    onError: (error, { recipe, action }) => {
      toast.error(explainLifecycleError(error instanceof ApiError ? error.problem.type : undefined, action === "publish" ? recipe.title : undefined));
    },
  });

  function request(recipe: RecipeListItemDto, action: RecipeAction) {
    if (actionConfirmation(action, recipe.title)) setPending({ recipe, action });
    else mutation.mutate({ recipe, action });
  }

  if (recipes.isPending) {
    return (
      <SkeletonGroup label="Đang tải công thức của bạn" className="dashboard-rows">
        {[0, 1, 2].map((row) => (
          <div className="dashboard-row" key={row}>
            <Skeleton width="76px" height="76px" />
            <div className="dashboard-row-body">
              <Skeleton width="55%" height="20px" />
              <Skeleton width="35%" height="13px" />
            </div>
            <Skeleton className="ui-skeleton-pill" width="96px" height="26px" />
          </div>
        ))}
      </SkeletonGroup>
    );
  }

  if (recipes.isError) {
    return (
      <Alert variant="error" title="Chưa thể tải công thức" action={<button type="button" onClick={() => void recipes.refetch()}>Thử lại</button>}>
        Vui lòng kiểm tra kết nối rồi thử lại.
      </Alert>
    );
  }

  if (recipes.data.length === 0) {
    return (
      <section className="dashboard-empty" aria-live="polite">
        <h2>Bạn chưa có công thức nào</h2>
        <p>Chia sẻ món đầu tiên của bạn với mọi người.</p>
        <Link className="btn btn-primary" href={routes.dashboardRecipeNew}>Viết công thức đầu tiên</Link>
      </section>
    );
  }

  const visible = filterByStatus(recipes.data, tab);
  const summary = summarizeMyRecipes(recipes.data);
  const confirmation = pending && actionConfirmation(pending.action, pending.recipe.title);

  return (
    <>
      <section className="dashboard-stats" aria-label="Tổng quan">
        <div className="rise dashboard-stat dashboard-stat-compact dashboard-stat-draft"><span>Bản nháp</span><strong>{summary.draft}</strong></div>
        <div className="rise dashboard-stat dashboard-stat-compact dashboard-stat-published"><span>Đã xuất bản</span><strong>{summary.published}</strong></div>
        <div className="rise dashboard-stat dashboard-stat-compact dashboard-stat-archived"><span>Lưu trữ</span><strong>{summary.archived}</strong></div>
      </section>
      <StatusTabs counts={{ all: recipes.data.length, ...summary }} />
      {visible.length === 0 ? (
        <section className="empty-state" aria-live="polite">
          <h2>Không có công thức ở mục này</h2>
          <p>Chọn mục khác hoặc tạo công thức mới.</p>
        </section>
      ) : (
        <ul className="dashboard-rows" aria-label="Công thức của tôi">
          {visible.map((recipe) => {
            const actions = availableActions(recipe.status);
            const main = actions.find((action) => action !== "archive" && action !== "delete");
            const gap = gaps.get(recipe.id);
            return (
              <li className="rw dashboard-row" key={recipe.id}>
                <RecipeImage
                  alt=""
                  className="dish ph dashboard-row-thumb"
                  height={76}
                  image={recipe.thumbnailUrl ? { thumbnailUrl: recipe.thumbnailUrl, mediumUrl: null, originalUrl: recipe.thumbnailUrl, url: recipe.thumbnailUrl } : null}
                  sizes="76px"
                  variant="thumbnail"
                  width={76}
                />
                <div className="dashboard-row-body">
                  <strong>{recipe.status === "Published" ? <Link href={recipeDetailHref(recipe)}>{recipe.title}</Link> : recipe.title}</strong>
                  <span>{recipe.categoryName} · Cập nhật {relativeTimeVi(recipe.updatedAt ?? recipe.createdAt).toLowerCase()}</span>
                  {gap && <span className="dashboard-row-warn">Còn thiếu: {gap}</span>}
                </div>
                <RecipeStatusBadge status={recipe.status} />
                <div className="dashboard-row-actions">
                  <Link className="btn btn-outline" href={`${routes.dashboardRecipes}/${recipe.id}/edit`}>Sửa</Link>
                  {main && (
                    <button type="button" className="btn btn-soft" disabled={mutation.isPending} onClick={() => request(recipe, main)}>
                      {actionLabels[main].verb}
                    </button>
                  )}
                  {actions.includes("archive") && (
                    <button type="button" className="btn btn-outline btn-icon" aria-label={`Lưu trữ ${recipe.title}`} disabled={mutation.isPending} onClick={() => request(recipe, "archive")}>
                      <ActionIcon path={actionIcons.archive} />
                    </button>
                  )}
                  <button type="button" className="btn btn-outline btn-icon is-danger" aria-label={`Xóa ${recipe.title}`} disabled={mutation.isPending} onClick={() => request(recipe, "delete")}>
                    <ActionIcon path={actionIcons.delete} />
                  </button>
                </div>
              </li>
            );
          })}
        </ul>
      )}
      <ConfirmDialog
        open={pending !== null}
        title={confirmation?.title ?? ""}
        description={confirmation?.description ?? ""}
        confirmLabel={confirmation?.confirmLabel ?? "Xác nhận"}
        destructive={confirmation?.destructive}
        icon={confirmation && <ConfirmDialogIcon path={confirmation.icon} tone={confirmation.tone} />}
        onCancel={() => setPending(null)}
        onConfirm={() => {
          if (pending) mutation.mutate(pending);
          setPending(null);
        }}
      />
    </>
  );
}
