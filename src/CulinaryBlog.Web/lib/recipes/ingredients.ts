import type { RecipeClient } from "./client.ts";
import type { ApiProblemDetails } from "../api/contracts.ts";
import { formToIngredientRequest } from "./form-mapper.ts";
import { MissingRecipeEtagError } from "./editor.ts";
import type { RecipeIngredientValues } from "./schema.ts";

export type RecipeIngredientMutation =
  | { kind: "delete"; ingredientId: string }
  | { kind: "update"; ingredientId: string; index: number; value: RecipeIngredientValues }
  | { kind: "create"; index: number; value: RecipeIngredientValues };

type IngredientRecipeClient = Pick<
  RecipeClient,
  "createIngredient" | "updateIngredient" | "deleteIngredient"
>;

export interface RecipeIngredientSyncResult {
  eTag: string;
  ingredients: RecipeIngredientValues[];
  persistedIngredients: RecipeIngredientValues[];
}

export type RecipeIngredientFieldName = "name" | "quantity" | "unit" | "notes";

export function mapIngredientProblemFieldErrors(
  problem: ApiProblemDetails,
): Partial<Record<RecipeIngredientFieldName, string>> {
  const result: Partial<Record<RecipeIngredientFieldName, string>> = {};
  const fields: Record<string, RecipeIngredientFieldName> = {
    name: "name",
    quantity: "quantity",
    unit: "unit",
    notes: "notes",
  };
  for (const [rawName, messages] of Object.entries(problem.errors ?? {})) {
    const normalized = rawName.toLowerCase().replace(/[^a-z]/g, "");
    const field = fields[normalized]
      ?? fields[normalized.replace(/^(request|command)/, "")];
    if (field && messages.length > 0) result[field] = messages.join(" ");
  }
  return result;
}

export class RecipeIngredientSyncError extends Error {
  readonly eTag: string;
  readonly failedMutation: RecipeIngredientMutation;
  readonly ingredients: RecipeIngredientValues[];
  readonly persistedIngredients: RecipeIngredientValues[];

  constructor({
    cause,
    eTag,
    failedMutation,
    ingredients,
    persistedIngredients,
  }: {
    cause: unknown;
    eTag: string;
    failedMutation: RecipeIngredientMutation;
    ingredients: RecipeIngredientValues[];
    persistedIngredients: RecipeIngredientValues[];
  }) {
    super("Ingredient synchronization stopped after a mutation failed.", { cause });
    this.eTag = eTag;
    this.failedMutation = failedMutation;
    this.ingredients = ingredients;
    this.persistedIngredients = persistedIngredients;
  }
}

export function diffRecipeIngredients(
  original: readonly RecipeIngredientValues[],
  current: readonly RecipeIngredientValues[],
): RecipeIngredientMutation[] {
  const currentIds = new Set(current.flatMap((ingredient) => ingredient.id ? [ingredient.id] : []));
  const originalById = new Map(
    original.flatMap((ingredient) => ingredient.id ? [[ingredient.id, ingredient] as const] : []),
  );
  const deleted: RecipeIngredientMutation[] = original.flatMap((ingredient) =>
    ingredient.id && !currentIds.has(ingredient.id)
      ? [{ kind: "delete" as const, ingredientId: ingredient.id }]
      : []);
  const updated: RecipeIngredientMutation[] = current.flatMap((ingredient, index) => {
    if (!ingredient.id) return [];
    const previous = originalById.get(ingredient.id);
    return previous && !sameIngredient(previous, ingredient)
      ? [{ kind: "update" as const, ingredientId: ingredient.id, index, value: ingredient }]
      : [];
  });
  const created: RecipeIngredientMutation[] = current.flatMap((ingredient, index) =>
    ingredient.id ? [] : [{ kind: "create" as const, index, value: ingredient }]);

  return [...deleted, ...updated, ...created];
}

export async function syncRecipeIngredients(
  client: IngredientRecipeClient,
  recipeId: string,
  startingEtag: string,
  original: readonly RecipeIngredientValues[],
  current: readonly RecipeIngredientValues[],
): Promise<RecipeIngredientSyncResult> {
  let eTag = startingEtag;
  let persistedIngredients = cloneIngredients(original);
  const ingredients = cloneIngredients(current);
  const mutations = diffRecipeIngredients(original, current);

  for (const mutation of mutations) {
    try {
      if (mutation.kind === "delete") {
        const response = await client.deleteIngredient(recipeId, mutation.ingredientId, eTag);
        eTag = requiredNextEtag(response.eTag);
        persistedIngredients = persistedIngredients.filter(
          (ingredient) => ingredient.id !== mutation.ingredientId,
        );
      } else if (mutation.kind === "update") {
        const response = await client.updateIngredient(
          recipeId,
          mutation.ingredientId,
          formToIngredientRequest(mutation.value),
          eTag,
        );
        eTag = requiredNextEtag(response.eTag);
        persistedIngredients = persistedIngredients.map((ingredient) =>
          ingredient.id === mutation.ingredientId ? { ...mutation.value } : ingredient);
      } else {
        const response = await client.createIngredient(
          recipeId,
          formToIngredientRequest(mutation.value),
          eTag,
        );
        eTag = requiredNextEtag(response.eTag);
        const created = { ...mutation.value, id: response.data.id };
        ingredients[mutation.index] = created;
        persistedIngredients.push(created);
      }
    } catch (cause) {
      throw new RecipeIngredientSyncError({
        cause,
        eTag,
        failedMutation: mutation,
        ingredients: cloneIngredients(ingredients),
        persistedIngredients: cloneIngredients(persistedIngredients),
      });
    }
  }

  return {
    eTag,
    ingredients,
    persistedIngredients: cloneIngredients(ingredients),
  };
}

function requiredNextEtag(eTag: string | null): string {
  if (!eTag?.trim()) throw new MissingRecipeEtagError();
  return eTag;
}

function sameIngredient(
  left: RecipeIngredientValues,
  right: RecipeIngredientValues,
): boolean {
  const leftRequest = formToIngredientRequest(left);
  const rightRequest = formToIngredientRequest(right);
  return leftRequest.name === rightRequest.name
    && leftRequest.quantity === rightRequest.quantity
    && leftRequest.unit === rightRequest.unit
    && leftRequest.notes === rightRequest.notes;
}

function cloneIngredients(
  ingredients: readonly RecipeIngredientValues[],
): RecipeIngredientValues[] {
  return ingredients.map((ingredient) => ({ ...ingredient }));
}
