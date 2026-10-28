import Link from "next/link";
import type { RecipeDetailViewModel } from "@/lib/recipes/detail";
import { RecipeDetailGallery } from "./recipe-detail-gallery";

export function RecipeDetail({ recipe }: { recipe: RecipeDetailViewModel }) {
  return (
    <div className="recipe-detail-page ds-scope">
      <MobileDetailHeader />
      <main className="recipe-detail-main">
        <nav className="recipe-detail-breadcrumb" aria-label="Breadcrumb">
          <Link href="/recipes">Công thức</Link>
          <span aria-hidden="true">/</span>
          <span aria-current="page">{recipe.title}</span>
        </nav>

        <article className="recipe-detail-article">
          <header className="recipe-detail-hero">
            <RecipeDetailGallery images={recipe.images} title={recipe.title} />
            <div className="recipe-detail-intro">
              <h1>{recipe.title}</h1>
              <p>{recipe.description}</p>
              <dl className="recipe-detail-facts">
                <Fact label="Chuẩn bị" value={recipe.prepTime} />
                <Fact label="Nấu" value={recipe.cookTime} />
                <Fact label="Khẩu phần" value={recipe.servings} />
                <Fact label="Độ khó" value={recipe.difficulty} />
              </dl>
            </div>
          </header>

          <div className="recipe-detail-body">
            <aside className="recipe-detail-ingredients" aria-labelledby="ingredients-heading">
              <h2 id="ingredients-heading">Nguyên liệu</h2>
              <ul>
                {recipe.ingredients.map((ingredient) => (
                  <li key={ingredient.id}>
                    <span>
                      {ingredient.name}
                      {ingredient.notes && <small>{ingredient.notes}</small>}
                    </span>
                    <strong>{ingredient.amount}</strong>
                  </li>
                ))}
              </ul>
            </aside>

            <div className="recipe-detail-method">
              <section aria-labelledby="steps-heading">
                <h2 id="steps-heading">Cách làm</h2>
                <ol className="recipe-detail-steps">
                  {recipe.steps.map((step) => (
                    <li key={step.id}>
                      <span className="recipe-detail-step-number" aria-hidden="true">{step.number}</span>
                      <div>
                        <p>{step.description}</p>
                        {step.timer && <span className="recipe-detail-timer">{step.timer}</span>}
                      </div>
                    </li>
                  ))}
                </ol>
              </section>

              {recipe.nutrition && (
                <section className="recipe-detail-nutrition" aria-labelledby="nutrition-heading">
                  <h2 id="nutrition-heading">Dinh dưỡng mỗi khẩu phần</h2>
                  <dl>
                    {recipe.nutrition.map((item) => (
                      <div key={item.label}>
                        <dd>{item.value}</dd>
                        <dt>{item.label}</dt>
                      </div>
                    ))}
                  </dl>
                </section>
              )}
            </div>
          </div>
        </article>
      </main>
    </div>
  );
}

function Fact({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt>{label}</dt>
      <dd>{value}</dd>
    </div>
  );
}

function MobileDetailHeader() {
  return (
    <header className="recipe-detail-mobile-header">
      <Link className="recipe-detail-mobile-action" href="/recipes" aria-label="Quay lại danh sách công thức">
        <svg aria-hidden="true" width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <path d="M15 5l-7 7 7 7" />
        </svg>
      </Link>
      <Link className="recipe-detail-mobile-brand" href="/">Culinary Blog</Link>
      <Link className="recipe-detail-mobile-action" href="/recipes" aria-label="Mở danh sách công thức">
        <svg aria-hidden="true" width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <path d="M4 7h16M4 12h16M4 17h16" />
        </svg>
      </Link>
    </header>
  );
}
