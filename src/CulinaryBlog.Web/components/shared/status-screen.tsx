import Link from "next/link";
import type { ReactNode } from "react";
import { routes } from "@/lib/navigation/routes";

type StatusKind = "404" | "403" | "500" | "401";

const screens: Record<StatusKind, { tint: string; ink: string; icon: string; title: string; text: string }> = {
  "404": {
    tint: "#fce9dd",
    ink: "#9a3412",
    icon: "M11 4a7 7 0 1 0 0 14 7 7 0 0 0 0-14zM20 20l-4-4M9 9l4 4M13 9l-4 4",
    title: "Món này không có trong thực đơn",
    text: "Đường dẫn bạn mở không tồn tại, hoặc công thức đã được tác giả gỡ xuống. Hãy thử tìm lại hoặc xem các công thức khác.",
  },
  "403": {
    tint: "#fbefcb",
    ink: "#5c4306",
    icon: "M6 11h12v9H6zM9 11V8a3 3 0 0 1 6 0v3",
    title: "Bạn chưa có quyền vào khu vực này",
    text: "Trang này chỉ dành cho quản trị viên. Nếu bạn nghĩ đây là nhầm lẫn, hãy liên hệ người quản trị.",
  },
  "500": {
    tint: "#f6dde0",
    ink: "#8a2a3a",
    icon: "M12 8v5M12 16h.01M4 20h16L12 4z",
    title: "Bếp đang gặp sự cố",
    text: "Máy chủ chưa xử lý được yêu cầu của bạn. Dữ liệu của bạn vẫn an toàn. Hãy thử lại sau ít phút.",
  },
  "401": {
    tint: "#e3eedf",
    ink: "#1f4a2b",
    icon: "M12 7v5l3 2M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18z",
    title: "Phiên đăng nhập đã hết hạn",
    text: "Vì lý do an toàn, bạn cần đăng nhập lại để tiếp tục.",
  },
};

/** The design's error screens: say what happened and always offer a way forward. */
export function StatusScreen({ kind, title, text, actions }: { kind: StatusKind; title?: string; text?: string; actions: ReactNode }) {
  const screen = screens[kind];
  return (
    <div className="status-screen">
      <section className="rise status-card" aria-labelledby="status-title">
        <span className="status-icon" aria-hidden="true" style={{ background: screen.tint, color: screen.ink }}>
          <svg width="38" height="38" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><path d={screen.icon} /></svg>
        </span>
        <span className="status-code" style={{ color: screen.ink }}>LỖI {kind}</span>
        <h1 id="status-title">{title ?? screen.title}</h1>
        <p>{text ?? screen.text}</p>
        <div className="status-actions">{actions}</div>
      </section>
    </div>
  );
}

export function NotFoundScreen({ title, text }: { title?: string; text?: string }) {
  return (
    <StatusScreen
      kind="404"
      title={title}
      text={text}
      actions={
        <>
          <Link className="btn btn-primary" href={routes.search}>Tìm công thức</Link>
          <Link className="btn btn-outline" href={routes.home}>Về trang chủ</Link>
        </>
      }
    />
  );
}

export function ServerErrorScreen({ onRetry, title, text }: { onRetry: () => void; title?: string; text?: string }) {
  return (
    <StatusScreen
      kind="500"
      title={title}
      text={text}
      actions={
        <>
          <button type="button" className="btn btn-primary" onClick={onRetry}>Thử lại</button>
          <Link className="btn btn-outline" href={routes.home}>Về trang chủ</Link>
        </>
      }
    />
  );
}
