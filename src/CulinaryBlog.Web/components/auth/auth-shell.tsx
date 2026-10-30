import Image from "next/image";
import Link from "next/link";
import type { ReactNode } from "react";
import { BrandLogo } from "@/components/shared/brand-logo";
import { MiniFooter } from "@/components/shared/site-footer";
import { routes } from "@/lib/navigation/routes";

/** Full-bleed food photo with the brand panel on the left and the form card on the right. */
export function AuthPhotoShell({
  photo,
  stackPhotos,
  blurb,
  photoCredit,
  children,
}: {
  photo: string;
  stackPhotos: [string, string, string];
  blurb: string;
  photoCredit: string;
  children: ReactNode;
}) {
  return (
    <div className="auth-photo-shell">
      <Image className="dish kb ph auth-photo" src={photo} alt="" fill sizes="100vw" priority />
      <div className="auth-photo-shade" aria-hidden="true" />
      <aside className="auth-panel">
        <Link href={routes.home} className="auth-brand"><BrandLogo size={46} />Culinary Blog</Link>
        <div className="auth-panel-copy">
          <div className="rise auth-stack">
            <span className="auth-stack-photos">
              {stackPhotos.map((src) => (
                <span className="pf" key={src}><Image src={src} alt="" width={42} height={42} /></span>
              ))}
            </span>
            <span><strong>Công thức Việt</strong><br /><small>đang được cộng đồng chia sẻ</small></span>
          </div>
          <h2>Mỗi công thức là một bữa cơm được chia sẻ.</h2>
          <p>{blurb}</p>
        </div>
        <span className="auth-panel-foot">Nhóm 07 · Phát triển ứng dụng web nâng cao<br /><small>{photoCredit}</small></span>
      </aside>
      <main className="auth-form-side">
        <div className="auth-form-center">{children}</div>
        <p className="auth-mobile-credit">{photoCredit}</p>
        <MiniFooter onDark />
      </main>
    </div>
  );
}

/** Quiet sand page for the forgot and reset password steps. */
export function AuthRecoveryShell({ children }: { children: ReactNode }) {
  return (
    <div className="auth-recovery-shell">
      <header className="auth-recovery-header">
        <Link href={routes.home} className="auth-brand"><BrandLogo size={46} />Culinary Blog</Link>
        <Link href={routes.login} className="auth-recovery-back">← Quay lại đăng nhập</Link>
      </header>
      <main className="auth-recovery-main">{children}</main>
      <MiniFooter />
    </div>
  );
}
