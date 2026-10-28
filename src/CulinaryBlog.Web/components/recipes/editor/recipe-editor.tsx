"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  useCallback,
  useEffect,
  useRef,
  useState,
} from "react";
import {
  useFieldArray,
  useForm,
  type FieldArrayWithId,
  type FieldErrors,
  type FieldPath,
  type SubmitErrorHandler,
} from "react-hook-form";
import { Alert, FormField, Skeleton, SkeletonGroup } from "@/components/shared";
import { ApiError, apiFetch } from "@/lib/api/client";
import type { CategoryDto } from "@/lib/api/contracts";
import { recipeClient } from "@/lib/recipes/client";
import {
  createRecipeEditorDefaults,
  mapRecipeProblemFieldErrors,
  MissingRecipeEtagError,
  moveRecipeEditorStep,
  recipeEditorConflictKind,
  recipeEditorErrorMessage,
  RECIPE_EDITOR_STEPS,
  RecipeCreatedContinuationError,
  saveRecipeBase,
  slugifyRecipeTitle,
  type RecipeEditorFieldName,
  type RecipeEditorStep,
} from "@/lib/recipes/editor";
import { recipeDetailToForm } from "@/lib/recipes/form-mapper";
import {
  mapIngredientProblemFieldErrors,
  RecipeIngredientSyncError,
  syncRecipeIngredients,
} from "@/lib/recipes/ingredients";
import {
  recipeEditorSchema,
  type RecipeEditorInput,
  type RecipeEditorValues,
  type RecipeIngredientValues,
  type RecipeStepValues,
} from "@/lib/recipes/schema";
import {
  mapStepProblemFieldErrors,
  RecipeStepSyncError,
  syncRecipeSteps,
} from "@/lib/recipes/steps";
import type { RecipeImageDto } from "@/lib/recipes/contracts";
import { RecipeImageManager } from "./recipe-image-manager";

const informationFields: FieldPath<RecipeEditorInput>[] = [
  "title",
  "slug",
  "description",
  "categoryId",
  "difficulty",
  "prepTimeMinutes",
  "cookTimeMinutes",
  "servings",
];

type EditorMode = "create" | "edit";

export function RecipeEditor({
  initialNotice,
  initialStep,
  mode,
  recipeId,
}: {
  initialNotice?: string;
  initialStep?: RecipeEditorStep;
  mode: EditorMode;
  recipeId?: string;
}) {
  const router = useRouter();
  const summaryRef = useRef<HTMLDivElement>(null);
  const slugWasEdited = useRef(mode === "edit");
  const persistedIngredients = useRef<RecipeIngredientValues[]>([]);
  const persistedSteps = useRef<RecipeStepValues[]>([]);
  const [step, setStep] = useState<RecipeEditorStep>(1);
  const [activeRecipeId, setActiveRecipeId] = useState(recipeId ?? null);
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [categoriesLoading, setCategoriesLoading] = useState(true);
  const [categoriesError, setCategoriesError] = useState("");
  const [recipeLoading, setRecipeLoading] = useState(mode === "edit");
  const [loadError, setLoadError] = useState("");
  const [submitError, setSubmitError] = useState("");
  const [notice, setNotice] = useState("");
  const [conflict, setConflict] = useState(false);
  const [eTag, setETag] = useState<string | null>(null);
  const [images, setImages] = useState<RecipeImageDto[]>([]);

  const {
    control,
    formState: { errors, isSubmitting },
    getValues,
    handleSubmit,
    register,
    reset,
    setError,
    setValue,
    trigger,
  } = useForm<RecipeEditorInput, unknown, RecipeEditorValues>({
    resolver: zodResolver(recipeEditorSchema),
    defaultValues: createRecipeEditorDefaults(),
    shouldUnregister: false,
    mode: "onBlur",
  });
  const ingredientFields = useFieldArray({
    control,
    name: "ingredients",
    keyName: "fieldKey",
  });
  const stepFields = useFieldArray({
    control,
    name: "steps",
    keyName: "fieldKey",
  });

  const loadCategories = useCallback(async () => {
    setCategoriesLoading(true);
    setCategoriesError("");
    try {
      setCategories(await apiFetch<CategoryDto[]>("/categories"));
    } catch {
      setCategoriesError("Không thể tải danh mục. Vui lòng thử lại.");
    } finally {
      setCategoriesLoading(false);
    }
  }, []);

  const loadRecipe = useCallback(async () => {
    if (mode !== "edit" || !recipeId) return;
    setRecipeLoading(true);
    setLoadError("");
    setSubmitError("");
    setConflict(false);
    try {
      const response = await recipeClient.detail(recipeId);
      if (!response.eTag) throw new MissingRecipeEtagError();
      const formValues = recipeDetailToForm(response.data);
      reset(formValues);
      persistedIngredients.current = formValues.ingredients.map((ingredient) => ({ ...ingredient }));
      persistedSteps.current = formValues.steps.map((recipeStep) => ({ ...recipeStep }));
      setActiveRecipeId(recipeId);
      setETag(response.eTag);
      setImages(response.data.images);
      slugWasEdited.current = true;
      setStep(initialStep ?? 1);
      setNotice(initialNotice ?? "");
    } catch (error) {
      setLoadError(error instanceof MissingRecipeEtagError
        ? "Máy chủ không trả về phiên bản công thức. Không thể chỉnh sửa an toàn."
        : recipeEditorErrorMessage(error));
    } finally {
      setRecipeLoading(false);
    }
  }, [initialNotice, initialStep, mode, recipeId, reset]);

  useEffect(() => { void loadCategories(); }, [loadCategories]);
  useEffect(() => { void loadRecipe(); }, [loadRecipe]);

  function focusSummary() {
    requestAnimationFrame(() => summaryRef.current?.focus());
  }

  async function goNext() {
    setSubmitError("");
    setNotice("");
    if (step === 1 && !await trigger(informationFields)) {
      setSubmitError("Vui lòng sửa các trường chưa hợp lệ trước khi tiếp tục.");
      focusSummary();
      return;
    }
    if (step === 2 && !await trigger("ingredients")) {
      setSubmitError("Vui lòng sửa các nguyên liệu chưa hợp lệ trước khi tiếp tục.");
      focusSummary();
      return;
    }
    if (step === 3 && !await trigger("steps")) {
      setSubmitError("Vui lòng sửa các bước thực hiện chưa hợp lệ trước khi tiếp tục.");
      focusSummary();
      return;
    }
    setStep(moveRecipeEditorStep(step, "next"));
  }

  function applyApiFieldErrors(error: ApiError): number {
    const mapped = mapRecipeProblemFieldErrors(error.problem);
    const mappedEntries = Object.entries(mapped);
    for (const [field, message] of mappedEntries) {
      setError(field as RecipeEditorFieldName, { type: "server", message });
    }
    if (mappedEntries.some(([field]) => !field.startsWith("nutrition."))) setStep(1);
    return mappedEntries.length;
  }

  const submit = async (values: RecipeEditorValues) => {
    setSubmitError("");
    setNotice("");
    setConflict(false);

    try {
      const result = await saveRecipeBase(
        recipeClient,
        activeRecipeId
          ? { mode: "edit", id: activeRecipeId, eTag, values }
          : { mode: "create", values },
      );
      const savedRecipeId = result.kind === "created" ? result.id : activeRecipeId;
      if (!savedRecipeId) throw new Error("Recipe id is unavailable after saving.");
      setActiveRecipeId(savedRecipeId);
      setETag(result.eTag);
      setImages(result.detail.images);

      const ingredientResult = await syncRecipeIngredients(
        recipeClient,
        savedRecipeId,
        result.eTag,
        persistedIngredients.current,
        values.ingredients,
      );
      setETag(ingredientResult.eTag);
      persistedIngredients.current = ingredientResult.persistedIngredients;
      ingredientFields.replace(ingredientResult.ingredients);

      const stepResult = await syncRecipeSteps(
        recipeClient,
        savedRecipeId,
        ingredientResult.eTag,
        persistedSteps.current,
        values.steps,
      );
      setETag(stepResult.eTag);
      persistedSteps.current = stepResult.persistedSteps;
      stepFields.replace(stepResult.steps);

      if (mode === "create") {
        router.replace(`/dashboard/recipes/${savedRecipeId}/edit?created=1`);
        return;
      }

      reset({
        ...values,
        ingredients: ingredientResult.ingredients,
        steps: stepResult.steps,
      });
      slugWasEdited.current = true;
      setNotice("Đã lưu thay đổi. Công thức vẫn giữ nguyên trạng thái hiện tại.");
      focusSummary();
    } catch (error) {
      if (error instanceof RecipeCreatedContinuationError) {
        router.replace(`/dashboard/recipes/${error.recipeId}/edit?created=partial`);
        return;
      }
      const underlyingError = error instanceof RecipeIngredientSyncError
        || error instanceof RecipeStepSyncError
        ? error.cause
        : error;
      if (error instanceof RecipeIngredientSyncError) {
        setETag(error.eTag);
        persistedIngredients.current = error.persistedIngredients;
        ingredientFields.replace(error.ingredients);
        setStep(2);
        if (underlyingError instanceof ApiError && error.failedMutation.kind !== "delete") {
          const fieldErrors = mapIngredientProblemFieldErrors(underlyingError.problem);
          for (const [field, errorMessage] of Object.entries(fieldErrors)) {
            setError(`ingredients.${error.failedMutation.index}.${field}` as FieldPath<RecipeEditorInput>, {
              type: "server",
              message: errorMessage,
            });
          }
          if (Object.keys(fieldErrors).length > 0) setStep(2);
        }
      }
      if (error instanceof RecipeStepSyncError) {
        setETag(error.eTag);
        persistedSteps.current = error.persistedSteps;
        stepFields.replace(error.steps);
        setStep(3);
        if (underlyingError instanceof ApiError && error.failedMutation.kind !== "delete") {
          const fieldErrors = mapStepProblemFieldErrors(underlyingError.problem);
          for (const [field, errorMessage] of Object.entries(fieldErrors)) {
            setError(`steps.${error.failedMutation.index}.${field}` as FieldPath<RecipeEditorInput>, {
              type: "server",
              message: errorMessage,
            });
          }
        }
      }
      if (recipeEditorConflictKind(underlyingError)) {
        setConflict(true);
        setSubmitError("Công thức đã thay đổi ở nơi khác hoặc phiên bản chỉnh sửa không còn hợp lệ.");
      } else if (underlyingError instanceof MissingRecipeEtagError) {
        setConflict(true);
        setSubmitError("Thiếu phiên bản công thức nên thay đổi chưa được gửi để tránh ghi đè dữ liệu.");
      } else {
        const fieldErrorCount = underlyingError instanceof ApiError
          ? applyApiFieldErrors(underlyingError)
          : 0;
        setSubmitError(fieldErrorCount > 0
          ? "Dữ liệu chưa hợp lệ. Vui lòng kiểm tra các trường được đánh dấu."
          : recipeEditorErrorMessage(underlyingError));
      }
      focusSummary();
    }
  };

  const invalid: SubmitErrorHandler<RecipeEditorInput> = (formErrors) => {
    setNotice("");
    setSubmitError("Vui lòng sửa các trường chưa hợp lệ trước khi lưu.");
    if (hasInformationError(formErrors)) setStep(1);
    else if (formErrors.ingredients) setStep(2);
    else if (formErrors.steps) setStep(3);
    focusSummary();
  };

  if (recipeLoading) return <RecipeEditorLoading />;

  return (
    <div className="recipe-editor-page">
      <EditorHeader mode={mode} />
      <main className="recipe-editor-main">
        {loadError ? (
          <Alert
            variant="error"
            title="Không thể mở trình chỉnh sửa"
            action={<button type="button" onClick={() => void loadRecipe()}>Thử lại</button>}
          >
            {loadError}
          </Alert>
        ) : (
          <form onSubmit={handleSubmit(submit, invalid)} noValidate>
            <EditorStepper currentStep={step} onSelect={setStep} />

            {(submitError || notice) && (
              <div className="recipe-editor-feedback" ref={summaryRef} tabIndex={-1}>
                {submitError && (
                  <Alert
                    variant="error"
                    title={conflict ? "Không thể lưu do xung đột phiên bản" : "Chưa thể lưu công thức"}
                    action={conflict && activeRecipeId
                      ? (
                          <button
                            type="button"
                            onClick={() => {
                              if (mode === "edit") void loadRecipe();
                              else router.replace(`/dashboard/recipes/${activeRecipeId}/edit`);
                            }}
                          >
                            Tải bản mới nhất
                          </button>
                        )
                      : undefined}
                  >
                    {submitError}
                  </Alert>
                )}
                {notice && <Alert variant="success">{notice}</Alert>}
              </div>
            )}

            <section className="recipe-editor-card" aria-labelledby={`recipe-editor-step-${step}`}>
              {step === 1 && (
                <InformationStep
                  categories={categories}
                  categoriesError={categoriesError}
                  categoriesLoading={categoriesLoading}
                  errors={errors}
                  getValues={getValues}
                  loadCategories={loadCategories}
                  register={register}
                  setValue={setValue}
                  slugWasEdited={slugWasEdited}
                />
              )}
              {step === 2 && (
                <IngredientsStep
                  errors={errors}
                  fields={ingredientFields.fields}
                  onAdd={() => ingredientFields.append({
                    name: "",
                    quantity: 1,
                    unit: "",
                    notes: undefined,
                  })}
                  onRemove={ingredientFields.remove}
                  register={register}
                />
              )}
              {step === 3 && (
                <StepsStep
                  errors={errors}
                  fields={stepFields.fields}
                  onAdd={() => stepFields.append({
                    description: "",
                    timerMinutes: undefined,
                    imageUrl: undefined,
                  })}
                  onRemove={stepFields.remove}
                  register={register}
                />
              )}
              {step === 4 && (
                <MediaAndNutritionStep
                  eTag={eTag}
                  errors={errors}
                  images={images}
                  onETagChange={setETag}
                  onImagesChange={setImages}
                  recipeId={activeRecipeId}
                  register={register}
                />
              )}
            </section>

            <div className="recipe-editor-actions">
              {step === 1 ? (
                <Link className="recipe-editor-button recipe-editor-button-secondary" href="/dashboard/recipes">
                  Hủy
                </Link>
              ) : (
                <button
                  className="recipe-editor-button recipe-editor-button-secondary"
                  type="button"
                  onClick={() => setStep(moveRecipeEditorStep(step, "previous"))}
                >
                  Quay lại
                </button>
              )}
              {step < 4 ? (
                // Distinct keys: reusing one DOM button while its type flips button -> submit makes the browser
                // submit the form from the very click that only meant to move to the last step.
                <button key="next" className="recipe-editor-button recipe-editor-button-primary" type="button" onClick={() => void goNext()}>
                  Tiếp tục
                </button>
              ) : (
                <button key="submit" className="recipe-editor-button recipe-editor-button-primary" type="submit" disabled={isSubmitting}>
                  {isSubmitting
                    ? "Đang lưu…"
                    : mode === "create" && !activeRecipeId
                      ? "Tạo bản nháp"
                      : "Lưu thay đổi"}
                </button>
              )}
            </div>
          </form>
        )}
      </main>
    </div>
  );
}

function EditorHeader({ mode }: { mode: EditorMode }) {
  return (
    <header className="recipe-editor-header">
      <div>
        <Link href="/dashboard/recipes">← Công thức của tôi</Link>
        <strong>{mode === "create" ? "Tạo công thức mới" : "Chỉnh sửa công thức"}</strong>
        <span>{mode === "create" ? "Lưu ở trạng thái bản nháp" : "Lưu an toàn theo phiên bản"}</span>
      </div>
    </header>
  );
}

function EditorStepper({
  currentStep,
  onSelect,
}: {
  currentStep: RecipeEditorStep;
  onSelect: (step: RecipeEditorStep) => void;
}) {
  return (
    <nav aria-label="Các bước tạo công thức">
      <ol className="recipe-editor-stepper">
        {RECIPE_EDITOR_STEPS.map((label, index) => {
          const number = index + 1 as RecipeEditorStep;
          const completed = number < currentStep;
          return (
            <li key={label}>
              <button
                type="button"
                aria-current={number === currentStep ? "step" : undefined}
                className="recipe-editor-step"
                data-completed={completed || undefined}
                onClick={() => onSelect(number)}
              >
                <span aria-hidden="true">{completed ? "✓" : number}</span>
                <span>{label}</span>
              </button>
            </li>
          );
        })}
      </ol>
    </nav>
  );
}

type InformationStepProps = {
  categories: CategoryDto[];
  categoriesError: string;
  categoriesLoading: boolean;
  errors: FieldErrors<RecipeEditorInput>;
  getValues: ReturnType<typeof useForm<RecipeEditorInput, unknown, RecipeEditorValues>>["getValues"];
  loadCategories: () => Promise<void>;
  register: ReturnType<typeof useForm<RecipeEditorInput, unknown, RecipeEditorValues>>["register"];
  setValue: ReturnType<typeof useForm<RecipeEditorInput, unknown, RecipeEditorValues>>["setValue"];
  slugWasEdited: React.MutableRefObject<boolean>;
};

function InformationStep({
  categories,
  categoriesError,
  categoriesLoading,
  errors,
  getValues,
  loadCategories,
  register,
  setValue,
  slugWasEdited,
}: InformationStepProps) {
  const titleRegistration = register("title");
  const slugRegistration = register("slug");

  return (
    <>
      <div className="recipe-editor-section-heading">
        <span>Bước 1</span>
        <h1 id="recipe-editor-step-1">Thông tin cơ bản</h1>
      </div>
      <FormField
        label="Tiêu đề"
        error={message(errors.title)}
        hint="1–200 ký tự. Slug sẽ được gợi ý từ tiêu đề và vẫn có thể chỉnh sửa."
        required
      >
        {(control) => (
          <input
            {...titleRegistration}
            {...control}
            maxLength={200}
            onChange={(event) => {
              void titleRegistration.onChange(event);
              if (!slugWasEdited.current) {
                setValue("slug", slugifyRecipeTitle(event.target.value), { shouldDirty: true });
              }
            }}
          />
        )}
      </FormField>
      <FormField label="Slug" error={message(errors.slug)} hint="Bắt buộc theo hợp đồng API hiện tại." required>
        {(control) => (
          <div className="recipe-editor-slug-control">
            <input
              {...slugRegistration}
              {...control}
              maxLength={220}
              onChange={(event) => {
                slugWasEdited.current = true;
                void slugRegistration.onChange(event);
              }}
            />
            <button
              type="button"
              onClick={() => {
                slugWasEdited.current = false;
                setValue("slug", slugifyRecipeTitle(String(getValues("title") ?? "")), {
                  shouldDirty: true,
                  shouldValidate: true,
                });
              }}
            >
              Tạo lại
            </button>
          </div>
        )}
      </FormField>
      <FormField label="Mô tả" error={message(errors.description)} required>
        {(control) => <textarea {...register("description")} {...control} rows={4} maxLength={5000} />}
      </FormField>
      <div className="recipe-editor-grid">
        <FormField label="Danh mục" error={message(errors.categoryId)} required>
          {(control) => (
            <select {...register("categoryId")} {...control} disabled={categoriesLoading}>
              <option value="">{categoriesLoading ? "Đang tải danh mục…" : "Chọn danh mục"}</option>
              {categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}
            </select>
          )}
        </FormField>
        <FormField label="Độ khó" error={message(errors.difficulty)} required>
          {(control) => (
            <select {...register("difficulty")} {...control}>
              <option value="Easy">Dễ</option>
              <option value="Medium">Trung bình</option>
              <option value="Hard">Khó</option>
            </select>
          )}
        </FormField>
        <FormField label="Chuẩn bị (phút)" error={message(errors.prepTimeMinutes)} required>
          {(control) => <input {...register("prepTimeMinutes")} {...control} type="number" inputMode="numeric" min={0} step={1} />}
        </FormField>
        <FormField label="Nấu (phút)" error={message(errors.cookTimeMinutes)} required>
          {(control) => <input {...register("cookTimeMinutes")} {...control} type="number" inputMode="numeric" min={0} step={1} />}
        </FormField>
        <FormField label="Khẩu phần" error={message(errors.servings)} required>
          {(control) => <input {...register("servings")} {...control} type="number" inputMode="numeric" min={1} step={1} />}
        </FormField>
      </div>
      {categoriesError && (
        <Alert variant="error" action={<button type="button" onClick={() => void loadCategories()}>Tải lại</button>}>
          {categoriesError}
        </Alert>
      )}
    </>
  );
}

function IngredientsStep({
  errors,
  fields,
  onAdd,
  onRemove,
  register,
}: {
  errors: FieldErrors<RecipeEditorInput>;
  fields: FieldArrayWithId<RecipeEditorInput, "ingredients", "fieldKey">[];
  onAdd: () => void;
  onRemove: (index: number) => void;
  register: ReturnType<typeof useForm<RecipeEditorInput, unknown, RecipeEditorValues>>["register"];
}) {
  return (
    <>
      <div className="recipe-editor-section-heading">
        <span>Bước 2</span>
        <h1 id="recipe-editor-step-2">Nguyên liệu</h1>
        <p>Có thể để trống khi lưu bản nháp. Cần ít nhất một nguyên liệu trước khi xuất bản.</p>
      </div>
      {fields.length === 0 ? (
        <div className="recipe-editor-ingredients-empty" role="status">
          Chưa có nguyên liệu. Bạn vẫn có thể lưu công thức dưới dạng bản nháp.
        </div>
      ) : (
        <div className="recipe-editor-ingredients">
          {fields.map((field, index) => {
            const fieldErrors = errors.ingredients?.[index];
            return (
              <fieldset className="recipe-editor-ingredient-row" key={field.fieldKey}>
                <legend>Nguyên liệu {index + 1}</legend>
                {field.id && <input type="hidden" {...register(`ingredients.${index}.id`)} />}
                <div className="recipe-editor-ingredient-main">
                  <FormField label="Tên" error={message(fieldErrors?.name)} required>
                    {(control) => (
                      <input
                        {...register(`ingredients.${index}.name`)}
                        {...control}
                        maxLength={200}
                      />
                    )}
                  </FormField>
                  <FormField label="Số lượng" error={message(fieldErrors?.quantity)} required>
                    {(control) => (
                      <input
                        {...register(`ingredients.${index}.quantity`)}
                        {...control}
                        type="number"
                        inputMode="decimal"
                        min="0.01"
                        step="any"
                      />
                    )}
                  </FormField>
                  <FormField label="Đơn vị" error={message(fieldErrors?.unit)} required>
                    {(control) => (
                      <input
                        {...register(`ingredients.${index}.unit`)}
                        {...control}
                        maxLength={50}
                      />
                    )}
                  </FormField>
                  <button
                    type="button"
                    className="recipe-editor-ingredient-remove"
                    aria-label={`Xóa nguyên liệu ${index + 1}`}
                    onClick={() => onRemove(index)}
                  >
                    ×
                  </button>
                </div>
                <FormField label="Ghi chú" error={message(fieldErrors?.notes)} hint="Không bắt buộc">
                  {(control) => (
                    <input
                      {...register(`ingredients.${index}.notes`)}
                      {...control}
                      maxLength={500}
                    />
                  )}
                </FormField>
              </fieldset>
            );
          })}
        </div>
      )}
      <button type="button" className="recipe-editor-add-row" onClick={onAdd}>
        Thêm nguyên liệu
      </button>
    </>
  );
}

function StepsStep({
  errors,
  fields,
  onAdd,
  onRemove,
  register,
}: {
  errors: FieldErrors<RecipeEditorInput>;
  fields: FieldArrayWithId<RecipeEditorInput, "steps", "fieldKey">[];
  onAdd: () => void;
  onRemove: (index: number) => void;
  register: ReturnType<typeof useForm<RecipeEditorInput, unknown, RecipeEditorValues>>["register"];
}) {
  return (
    <>
      <div className="recipe-editor-section-heading">
        <span>Bước 3</span>
        <h1 id="recipe-editor-step-3">Các bước thực hiện</h1>
        <p>
          Số bước do máy chủ quản lý theo thứ tự tạo. Khi xóa, các bước còn lại được đánh số lại tự động.
        </p>
      </div>
      {fields.length === 0 ? (
        <div className="recipe-editor-steps-empty" role="status">
          Chưa có bước thực hiện. Bạn vẫn có thể lưu công thức dưới dạng bản nháp.
        </div>
      ) : (
        <div className="recipe-editor-steps-list">
          {fields.map((field, index) => {
            const fieldErrors = errors.steps?.[index];
            return (
              <fieldset className="recipe-editor-step-row" key={field.fieldKey}>
                <legend className="visually-hidden">Bước thực hiện {index + 1}</legend>
                {field.id && <input type="hidden" {...register(`steps.${index}.id`)} />}
                <span className="recipe-editor-step-number" aria-hidden="true">{index + 1}</span>
                <div className="recipe-editor-step-fields">
                  <FormField
                    label={`Mô tả bước ${index + 1}`}
                    error={message(fieldErrors?.description)}
                    required
                  >
                    {(control) => (
                      <textarea
                        {...register(`steps.${index}.description`)}
                        {...control}
                        rows={4}
                        maxLength={2000}
                      />
                    )}
                  </FormField>
                  <div className="recipe-editor-step-options">
                    <FormField label="Hẹn giờ (phút)" error={message(fieldErrors?.timerMinutes)}>
                      {(control) => (
                        <input
                          {...register(`steps.${index}.timerMinutes`)}
                          {...control}
                          type="number"
                          inputMode="numeric"
                          min={0}
                          step={1}
                        />
                      )}
                    </FormField>
                    <FormField
                      label="URL ảnh bước"
                      error={message(fieldErrors?.imageUrl)}
                      hint="Không bắt buộc; tải ảnh trực tiếp chưa thuộc giai đoạn này."
                    >
                      {(control) => (
                        <input
                          {...register(`steps.${index}.imageUrl`)}
                          {...control}
                          type="url"
                          maxLength={2048}
                          placeholder="https://…"
                        />
                      )}
                    </FormField>
                  </div>
                </div>
                <button
                  type="button"
                  className="recipe-editor-step-remove"
                  aria-label={`Xóa bước ${index + 1}`}
                  onClick={() => onRemove(index)}
                >
                  ×
                </button>
              </fieldset>
            );
          })}
        </div>
      )}
      <button type="button" className="recipe-editor-add-row" onClick={onAdd}>
        Thêm bước
      </button>
    </>
  );
}

function MediaAndNutritionStep({
  eTag,
  errors,
  images,
  onETagChange,
  onImagesChange,
  recipeId,
  register,
}: {
  eTag: string | null;
  errors: FieldErrors<RecipeEditorInput>;
  images: RecipeImageDto[];
  onETagChange: (eTag: string) => void;
  onImagesChange: (images: RecipeImageDto[]) => void;
  recipeId: string | null;
  register: ReturnType<typeof useForm<RecipeEditorInput, unknown, RecipeEditorValues>>["register"];
}) {
  return (
    <>
      <div className="recipe-editor-section-heading">
        <span>Bước 4</span>
        <h1 id="recipe-editor-step-4">Ảnh và dinh dưỡng</h1>
      </div>
      <RecipeImageManager
        recipeId={recipeId}
        eTag={eTag}
        images={images}
        onETagChange={onETagChange}
        onImagesChange={onImagesChange}
      />
      <fieldset className="recipe-editor-nutrition">
        <legend>Dinh dưỡng mỗi khẩu phần <span>(không bắt buộc)</span></legend>
        <div className="recipe-editor-grid recipe-editor-grid-nutrition">
          <FormField label="Năng lượng (kcal)" error={message(errors.nutrition?.calories)}>
            {(control) => <input {...register("nutrition.calories")} {...control} type="number" inputMode="numeric" min={0} step={1} />}
          </FormField>
          <FormField label="Đạm (g)" error={message(errors.nutrition?.proteinGrams)}>
            {(control) => <input {...register("nutrition.proteinGrams")} {...control} type="number" inputMode="decimal" min={0} step="any" />}
          </FormField>
          <FormField label="Carb (g)" error={message(errors.nutrition?.carbohydratesGrams)}>
            {(control) => <input {...register("nutrition.carbohydratesGrams")} {...control} type="number" inputMode="decimal" min={0} step="any" />}
          </FormField>
          <FormField label="Béo (g)" error={message(errors.nutrition?.fatGrams)}>
            {(control) => <input {...register("nutrition.fatGrams")} {...control} type="number" inputMode="decimal" min={0} step="any" />}
          </FormField>
        </div>
      </fieldset>
    </>
  );
}

function RecipeEditorLoading() {
  return (
    <div className="recipe-editor-page">
      <EditorHeader mode="edit" />
      <main className="recipe-editor-main">
        <SkeletonGroup className="recipe-editor-loading" label="Đang tải công thức để chỉnh sửa">
          <Skeleton height="4rem" />
          <Skeleton height="28rem" />
        </SkeletonGroup>
      </main>
    </div>
  );
}

function message(error: { message?: unknown } | undefined): string | undefined {
  return typeof error?.message === "string" ? error.message : undefined;
}

function hasInformationError(errors: FieldErrors<RecipeEditorInput>): boolean {
  return informationFields.some((field) => field in errors);
}
