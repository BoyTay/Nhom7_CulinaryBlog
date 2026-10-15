import type { ApiProblemDetails } from "../api/contracts.ts";
import type { RecipeClient } from "./client.ts";
import { MissingRecipeEtagError } from "./editor.ts";
import { formToStepRequest } from "./form-mapper.ts";
import type { RecipeStepValues } from "./schema.ts";

export type RecipeStepMutation =
  | { kind: "delete"; stepId: string }
  | { kind: "update"; stepId: string; index: number; value: RecipeStepValues }
  | { kind: "create"; index: number; value: RecipeStepValues };

type StepRecipeClient = Pick<RecipeClient, "createStep" | "updateStep" | "deleteStep">;

export interface RecipeStepSyncResult {
  eTag: string;
  steps: RecipeStepValues[];
  persistedSteps: RecipeStepValues[];
}

export type RecipeStepFieldName = "description" | "timerMinutes" | "imageUrl";

export function mapStepProblemFieldErrors(
  problem: ApiProblemDetails,
): Partial<Record<RecipeStepFieldName, string>> {
  const result: Partial<Record<RecipeStepFieldName, string>> = {};
  const fields: Record<string, RecipeStepFieldName> = {
    description: "description",
    timerminutes: "timerMinutes",
    imageurl: "imageUrl",
  };
  for (const [rawName, messages] of Object.entries(problem.errors ?? {})) {
    const normalized = rawName.toLowerCase().replace(/[^a-z]/g, "");
    const field = fields[normalized]
      ?? fields[normalized.replace(/^(request|command)/, "")];
    if (field && messages.length > 0) result[field] = messages.join(" ");
  }
  return result;
}

export class RecipeStepSyncError extends Error {
  readonly eTag: string;
  readonly failedMutation: RecipeStepMutation;
  readonly steps: RecipeStepValues[];
  readonly persistedSteps: RecipeStepValues[];

  constructor({
    cause,
    eTag,
    failedMutation,
    steps,
    persistedSteps,
  }: {
    cause: unknown;
    eTag: string;
    failedMutation: RecipeStepMutation;
    steps: RecipeStepValues[];
    persistedSteps: RecipeStepValues[];
  }) {
    super("Step synchronization stopped after a mutation failed.", { cause });
    this.eTag = eTag;
    this.failedMutation = failedMutation;
    this.steps = steps;
    this.persistedSteps = persistedSteps;
  }
}

export function diffRecipeSteps(
  original: readonly RecipeStepValues[],
  current: readonly RecipeStepValues[],
): RecipeStepMutation[] {
  const currentIds = new Set(current.flatMap((step) => step.id ? [step.id] : []));
  const originalById = new Map(
    original.flatMap((step) => step.id ? [[step.id, step] as const] : []),
  );
  const deleted: RecipeStepMutation[] = original.flatMap((step) =>
    step.id && !currentIds.has(step.id)
      ? [{ kind: "delete" as const, stepId: step.id }]
      : []);
  const updated: RecipeStepMutation[] = current.flatMap((step, index) => {
    if (!step.id) return [];
    const previous = originalById.get(step.id);
    return previous && !sameStep(previous, step)
      ? [{ kind: "update" as const, stepId: step.id, index, value: step }]
      : [];
  });
  const created: RecipeStepMutation[] = current.flatMap((step, index) =>
    step.id ? [] : [{ kind: "create" as const, index, value: step }]);

  return [...deleted, ...updated, ...created];
}

export async function syncRecipeSteps(
  client: StepRecipeClient,
  recipeId: string,
  startingEtag: string,
  original: readonly RecipeStepValues[],
  current: readonly RecipeStepValues[],
): Promise<RecipeStepSyncResult> {
  let eTag = startingEtag;
  let persistedSteps = cloneSteps(original);
  const steps = cloneSteps(current);
  const mutations = diffRecipeSteps(original, current);

  for (const mutation of mutations) {
    try {
      if (mutation.kind === "delete") {
        const response = await client.deleteStep(recipeId, mutation.stepId, eTag);
        eTag = requiredNextEtag(response.eTag);
        persistedSteps = persistedSteps.filter((step) => step.id !== mutation.stepId);
      } else if (mutation.kind === "update") {
        const response = await client.updateStep(
          recipeId,
          mutation.stepId,
          formToStepRequest(mutation.value),
          eTag,
        );
        eTag = requiredNextEtag(response.eTag);
        persistedSteps = persistedSteps.map((step) =>
          step.id === mutation.stepId ? { ...mutation.value } : step);
      } else {
        const response = await client.createStep(
          recipeId,
          formToStepRequest(mutation.value),
          eTag,
        );
        eTag = requiredNextEtag(response.eTag);
        const created = { ...mutation.value, id: response.data.id };
        steps[mutation.index] = created;
        persistedSteps.push(created);
      }
    } catch (cause) {
      throw new RecipeStepSyncError({
        cause,
        eTag,
        failedMutation: mutation,
        steps: cloneSteps(steps),
        persistedSteps: cloneSteps(persistedSteps),
      });
    }
  }

  return {
    eTag,
    steps,
    persistedSteps: cloneSteps(steps),
  };
}

function requiredNextEtag(eTag: string | null): string {
  if (!eTag?.trim()) throw new MissingRecipeEtagError();
  return eTag;
}

function sameStep(left: RecipeStepValues, right: RecipeStepValues): boolean {
  const leftRequest = formToStepRequest(left);
  const rightRequest = formToStepRequest(right);
  return leftRequest.description === rightRequest.description
    && leftRequest.timerMinutes === rightRequest.timerMinutes
    && leftRequest.imageUrl === rightRequest.imageUrl;
}

function cloneSteps(steps: readonly RecipeStepValues[]): RecipeStepValues[] {
  return steps.map((step) => ({ ...step }));
}
