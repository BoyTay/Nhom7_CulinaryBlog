import type { Metadata } from "next";
import type { ReactNode } from "react";
import { AuthProvider } from "@/lib/auth/auth-context";
import QueryProvider from "@/lib/query/query-provider";
import { ToastProvider } from "@/components/shared/toast";
import { AppChrome } from "@/components/shared/app-chrome";
// Self-hosted (bundled) fonts: next/font/google fetched Google Fonts at build time and broke CI.
// The design uses Fraunces with its optical size axis; static cuts run up to 9% wider at display sizes.
import "@fontsource-variable/fraunces/opsz.css";
import "@fontsource/be-vietnam-pro/latin-400.css";
import "@fontsource/be-vietnam-pro/latin-500.css";
import "@fontsource/be-vietnam-pro/latin-600.css";
import "@fontsource/be-vietnam-pro/latin-700.css";
import "@fontsource/be-vietnam-pro/vietnamese-400.css";
import "@fontsource/be-vietnam-pro/vietnamese-500.css";
import "@fontsource/be-vietnam-pro/vietnamese-600.css";
import "@fontsource/be-vietnam-pro/vietnamese-700.css";
import "./globals.css";
import "./motion.css";
import "@/components/ui/ui.css";
import "@/components/shared/shared.css";

export const metadata: Metadata = {
  title: {
    default: "Culinary Blog",
    template: "%s | Culinary Blog",
  },
  description: "Khám phá, chia sẻ và lưu giữ những công thức đáng nhớ.",
  icons: {
    icon: [
      { url: "/images/brand/logo-32.png", sizes: "32x32", type: "image/png" },
      { url: "/images/brand/logo-192.png", sizes: "192x192", type: "image/png" },
    ],
    shortcut: "/images/brand/favicon.ico",
  },
};

export default function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <html lang="vi">
      <body>
        <QueryProvider>
          <AuthProvider>
            <ToastProvider>
              <AppChrome>{children}</AppChrome>
            </ToastProvider>
          </AuthProvider>
        </QueryProvider>
      </body>
    </html>
  );
}

