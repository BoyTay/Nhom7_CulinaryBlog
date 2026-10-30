import Link from "next/link";
import type { RecipeDetailViewModel } from "@/lib/recipes/detail";
import type { RecipeCardModel } from "@/lib/recipes/list";
import { routes } from "@/lib/navigation/routes";
import { RecipeActions, RecipeMobileBar, StepTimer } from "./recipe-detail-actions";
import { RecipeDetailGallery } from "./recipe-detail-gallery";
import { RelatedRecipes } from "./related-recipes";

const factIcons = {
  prep: "M4 20h16M7 20V9m10 11V9M4 9h16l-2-5H6z",
  cook: "M12 3c1 3-2 4-2 7a3 3 0 0 0 6 0c0-1-.5-2-1-3 3 2 4 5 4 8a7 7 0 0 1-14 0c0-4 3-6 7-12z",
  serving: "M16 11a4 4 0 1 0-8 0M4 21a8 8 0 0 1 16 0",
  level: "M5 20V12M12 20V6M19 20V9",
};
const nutritionColors = ["#c2410c", "#2f5d3a", "#e9b44c", "#9a3412"];

export function RecipeDetail({ recipe, related = [] }: { recipe: RecipeDetailViewModel; related?: RecipeCardModel[] }) {
  return (
    <div className="recipe-detail-page">
      <RecipeMobileBar title={recipe.title} categoryName={recipe.categoryName} backHref={recipe.categoryHref} />
      <main className="recipe-detail-main">
        <article className="recipe-detail-article">
          <nav className="breadcrumb recipe-detail-breadcrumb" aria-label="Đường dẫn">
            <Link href={routes.home}>Trang chủ</Link>
            <span aria-hidden="true">/</span>
            <Link href={routes.recipes}>Công thức</Link>
            <span aria-hidden="true">/</span>
            <Link href={recipe.categoryHref}>{recipe.categoryName}</Link>
            <span aria-hidden="true">/</span>
            <span aria-current="page">{recipe.title}</span>
          </nav>

          <header className="recipe-detail-header">
            <div className="recipe-detail-intro">
              <Link className="recipe-detail-category" href={recipe.categoryHref}>{recipe.categoryName}</Link>
              <h1 className="rise">{recipe.title}</h1>
              <p>{recipe.description}</p>
              <div className="recipe-detail-author">
                <span aria-hidden="true">{recipe.authorInitials}</span>
                <span>
                  <strong>{recipe.authorName}</strong>
                  {recipe.publishedLabel && <small>{recipe.publishedLabel}</small>}
                </span>
              </div>
            </div>
            <RecipeActions title={recipe.title} />
          </header>

          <RecipeDetailGallery images={recipe.images} title={recipe.title} />

          <section aria-label="Thông tin nhanh" className="rv recipe-detail-facts">
            <Fact label="Chuẩn bị" value={recipe.prepTime} icon={factIcons.prep} tint="#e3eedf" ink="#1f4a2b" />
            <Fact label="Nấu" value={recipe.cookTime} icon={factIcons.cook} tint="#fce9dd" ink="#9a3412" />
            <Fact label="Khẩu phần" value={recipe.servings} icon={factIcons.serving} tint="#dde9f0" ink="#1f4a63" />
            <Fact label="Độ khó" value={recipe.difficulty} icon={factIcons.level} tint="#fbefcb" ink="#5c4306" />
          </section>

          <nav aria-label="Phần công thức" className="noprint recipe-detail-switch">
            <a href="#ingredients-heading">Nguyên liệu</a>
            <a href="#steps-heading">Cách làm</a>
          </nav>

          <div className="recipe-detail-body">
            <aside className="slide recipe-detail-ingredients" aria-labelledby="ingredients-heading">
              <div className="recipe-detail-ingredients-head">
                <h2 id="ingredients-heading">Nguyên liệu</h2>
                <span>{recipe.ingredients.length} thứ · {recipe.servings}</span>
              </div>
              <p>Chạm để đánh dấu những thứ bạn đã có.</p>
              <ul>
                {recipe.ingredients.map((ingredient) => (
                  <li key={ingredient.id}>
                    <label className="ing">
                      <input type="checkbox" />
                      <span>
                        {ingredient.name}
                        {ingredient.notes && <small>{ingredient.notes}</small>}
                      </span>
                      <strong>{ingredient.amount}</strong>
                    </label>
                  </li>
                ))}
              </ul>
            </aside>

            <div className="recipe-detail-method">
              <section aria-labelledby="steps-heading">
                <h2 id="steps-heading">Cách làm</h2>
                <ol className="recipe-detail-steps">
                  {recipe.steps.map((step) => (
                    <li key={step.id} className="rv">
                      <span className="recipe-detail-step-number" aria-hidden="true">{step.number}</span>
                      <div>
                        <p>{step.description}</p>
                        {step.timerMinutes !== undefined && <StepTimer minutes={step.timerMinutes} />}
                      </div>
                    </li>
                  ))}
                </ol>
              </section>

              {recipe.nutrition && (
                <section className="recipe-detail-nutrition" aria-labelledby="nutrition-heading">
                  <div className="recipe-detail-nutrition-head">
                    <h2 id="nutrition-heading">Giá trị dinh dưỡng</h2>
                    <span>Ước tính cho 1 khẩu phần</span>
                  </div>
                  <dl>
                    {recipe.nutrition.map((item, index) => (
                      <div key={item.label}>
                        <dt>{item.label}</dt>
                        <dd><NutritionValue value={item.value} /></dd>
                        <span aria-hidden="true" className="recipe-detail-bar">
                          <span className="grow" style={{ width: `${item.percent}%`, background: nutritionColors[index % nutritionColors.length] }} />
                        </span>
                      </div>
                    ))}
                  </dl>
                </section>
              )}
            </div>
          </div>
        </article>

        <RelatedRecipes cards={related} />

        {recipe.steps[0] && (
          <div className="noprint recipe-mobile-start">
            <span>
              <strong>Bước 1 · {recipe.steps[0].description}</strong>
              {recipe.steps[0].timerMinutes ? `Hẹn giờ ${recipe.steps[0].timerMinutes} phút` : `${recipe.steps.length} bước`}
            </span>
            <a className="btn btn-primary" href="#steps-heading">Bắt đầu</a>
          </div>
        )}
      </main>
    </div>
  );
}

/** The amount is set in the display face, its unit ("kcal", "g") small in the body face. */
function NutritionValue({ value }: { value: string }) {
  const split = value.lastIndexOf(" ");
  return <>{value.slice(0, split)}<small> {value.slice(split + 1)}</small></>;
}

function Fact({ label, value, icon, tint, ink }: { label: string; value: string; icon: string; tint: string; ink: string }) {
  return (
    <div className="recipe-detail-fact">
      <span style={{ background: tint, color: ink }}>
        <svg aria-hidden="true" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><path d={icon} /></svg>
      </span>
      <span>
        <small>{label}</small>
        <strong>{value}</strong>
      </span>
    </div>
  );
}
