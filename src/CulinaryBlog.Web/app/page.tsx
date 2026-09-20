const features = [
  {
    eyebrow: "Khám phá",
    title: "Công thức dễ tìm",
    description: "Tìm theo nguyên liệu, danh mục và độ khó với kết quả được sắp xếp rõ ràng.",
  },
  {
    eyebrow: "Chia sẻ",
    title: "Căn bếp của riêng bạn",
    description: "Lưu từng bước, nguyên liệu và hình ảnh trong một quy trình biên soạn mạch lạc.",
  },
  {
    eyebrow: "Tin cậy",
    title: "Nội dung có cấu trúc",
    description: "Thông tin dinh dưỡng, thời gian và khẩu phần được trình bày nhất quán trên mọi thiết bị.",
  },
];

export default function HomePage() {
  return (
    <main>
      <header className="site-header">
        <a className="brand" href="#top" aria-label="Culinary Blog — trang chủ">
          <span className="brand-mark" aria-hidden="true">C</span>
          <span>Culinary Blog</span>
        </a>
        <nav aria-label="Điều hướng chính">
          <a href="#features">Khám phá</a>
          <a href="#about">Giới thiệu</a>
        </nav>
      </header>

      <section className="hero" id="top">
        <div className="hero-copy">
          <p className="kicker">Từ căn bếp đến cộng đồng</p>
          <h1>Mỗi món ăn đều có một câu chuyện đáng sẻ chia.</h1>
          <p className="lede">
            Một không gian gọn gàng để khám phá công thức mới, ghi lại bí quyết gia đình
            và truyền cảm hứng cho bữa ăn tiếp theo.
          </p>
          <div className="hero-actions" aria-label="Bắt đầu">
            <a className="button button-primary" href="#features">Khám phá nền tảng</a>
            <a className="button button-secondary" href="/scalar">Xem API</a>
          </div>
        </div>
        <div className="hero-panel" aria-label="Thông tin nền tảng">
          <p className="panel-label">Kiến trúc sẵn sàng</p>
          <dl>
            <div><dt>Backend</dt><dd>.NET 10</dd></div>
            <div><dt>Frontend</dt><dd>Next.js 15</dd></div>
            <div><dt>Dữ liệu</dt><dd>PostgreSQL 16</dd></div>
          </dl>
        </div>
      </section>

      <section className="features" id="features" aria-labelledby="features-title">
        <div className="section-heading">
          <p className="kicker">Nền tảng dùng chung</p>
          <h2 id="features-title">Được thiết kế để nội dung là nhân vật chính.</h2>
        </div>
        <div className="feature-grid">
          {features.map((feature) => (
            <article className="feature-card" key={feature.title}>
              <p>{feature.eyebrow}</p>
              <h3>{feature.title}</h3>
              <span>{feature.description}</span>
            </article>
          ))}
        </div>
      </section>

      <footer id="about">
        <p>Nhóm 7 · Advanced Web Application Development · 2026–2027</p>
      </footer>
    </main>
  );
}
