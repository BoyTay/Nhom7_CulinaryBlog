"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { useState } from "react";
import { Alert, Skeleton, useToast } from "@/components/shared";
import { ApiError } from "@/lib/api/client";
import {
  actionLabels,
  availableActions,
  explainLifecycleError,
  filterByStatus,
  runRecipeAction,
  summarizeMyRecipes,
  type RecipeAction,
} from "@/lib/dashboard/my-recipes";
import { RecipeImage } from "@/components/recipes/recipe-image";
import { recipeClient } from "@/lib/recipes/client";
import type { RecipeListItemDto } from "@/lib/recipes/contracts";
import { recipeDetailHref, routes } from "@/lib/navigation/routes";
import { formatDateVi } from "@/lib/ui/format";
import { ConfirmDialog } from "./confirm-dialog";
import { RecipeStatusBadge } from "./recipe-status-badge";
import { StatusTabs, useStatusTab } from "./status-tabs";

export const myRecipesQueryKey = ["recipes", "mine"] as const;

export function useMyRecipes() {
  return useQuery({
    queryKey: myRecipesQueryKey,
    queryFn: async () => (await recipeClient.listMine()).data,
  });
}

interface PendingAction {
  recipe: RecipeListItemDto;
  action: RecipeAction;
}

const needsConfirmation: ReadonlySet<RecipeAction> = new Set(["archive", "delete"]);

export function MyRecipesTable() {
  const recipes = useMyRecipes();
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
    onError: (error) => {
      toast.error(explainLifecycleError(error instanceof ApiError ? error.problem.type : undefined));
    },
  });

  function request(recipe: RecipeListItemDto, action: RecipeAction) {
    if (needsConfirmation.has(action)) setPending({ recipe, action });
    else mutation.mutate({ recipe, action });
  }

  if (recipes.isPending) {
    return (
      <div aria-busy="true" aria-label="Đang tải công thức của bạn">
        <Skeleton width="100%" height="3rem" />
        <Skeleton width="100%" height="3rem" />
        <Skeleton width="100%" height="3rem" />
      </div>
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

  return (
    <>
      <section className="dashboard-stats" aria-label="Tổng quan">
        <div className="rise dashboard-stat dashboard-stat-compact dashboard-stat-draft"><span>Bản nháp</span><strong>{summary.draft}</strong></div>
        <div className="rise dashboard-stat dashboard-stat-compact dashboard-stat-published"><span>Đã xuất bản</span><strong>{summary.published}</strong></div>
        <div className="rise dashboard-stat dashboard-stat-compact dashboard-stat-archived"><span>Lưu trữ</span><strong>{summary.archived}</strong></div>
      </section>
      <StatusTabs />
      {visible.length === 0 ? (
        <section className="empty-state" aria-live="polite">
          <h2>Không có công thức ở mục này</h2>
          <p>Chọn mục khác hoặc tạo công thức mới.</p>
        </section>
      ) : (
        <ul className="dashboard-rows" aria-label="Công thức của tôi">
          {visible.map((recipe) => (
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
                <strong>{recipe.title}</strong>
                <span>{recipe.categoryName} · Cập nhật {formatDateVi(recipe.updatedAt ?? recipe.createdAt)}</span>
              </div>
              <RecipeStatusBadge status={recipe.status} />
              <div className="dashboard-row-actions">
                <Link className="btn btn-outline" href={`${routes.dashboardRecipes}/${recipe.id}/edit`}>Sửa</Link>
                {recipe.status === "Published" && <Link className="btn btn-outline" href={recipeDetailHref(recipe)}>Xem</Link>}
                {availableActions(recipe.status).map((action) => (
                  <button
                    key={action}
                    type="button"
                    className={action === "delete" ? "btn btn-outline is-danger" : "btn btn-soft"}
                    aria-label={action === "delete" ? `Xóa ${recipe.title}` : undefined}
                    disabled={mutation.isPending}
                    onClick={() => request(recipe, action)}
                  >
                    {actionLabels[action].verb}
                  </button>
                ))}
              </div>
            </li>
          ))}
        </ul>
      )}
      <p className="dashboard-note">Chỉ có thể xuất bản khi công thức có ít nhất 1 nguyên liệu và 1 bước thực hiện.</p>
      <ConfirmDialog
        open={pending !== null}
        title={pending ? `${actionLabels[pending.action].verb} công thức?` : ""}
        description={pending ? `“${pending.recipe.title}” sẽ được ${actionLabels[pending.action].verb.toLowerCase()}.` : ""}
        confirmLabel={pending ? actionLabels[pending.action].verb : "Xác nhận"}
        destructive={pending?.action === "delete"}
        onCancel={() => setPending(null)}
        onConfirm={() => {
          if (pending) mutation.mutate(pending);
          setPending(null);
        }}
      />
    </>
  );
}
