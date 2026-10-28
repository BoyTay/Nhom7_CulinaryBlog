import type { Metadata } from "next";
import type { ReactNode } from "react";
import { Be_Vietnam_Pro, Bricolage_Grotesque } from "next/font/google";
import { AuthProvider } from "@/lib/auth/auth-context";
import QueryProvider from "@/lib/query/query-provider";
import { ToastProvider } from "@/components/shared/toast";
import { AppChrome } from "@/components/shared/app-chrome";
import "./globals.css";
import "@/components/shared/shared.css";

export const metadata: Metadata = {
  title: {
    default: "Culinary Blog",
    template: "%s | Culinary Blog",
  },
  description: "Khám phá, chia sẻ và lưu giữ những công thức đáng nhớ.",
};

const headingFont = Bricolage_Grotesque({
  subsets: ["latin", "vietnamese"],
  weight: ["600", "700"],
  variable: "--font-heading",
  display: "swap",
});

const bodyFont = Be_Vietnam_Pro({
  subsets: ["latin", "vietnamese"],
  weight: ["400", "500", "600"],
  variable: "--font-body",
  display: "swap",
});

export default function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <html lang="vi" className={`${headingFont.variable} ${bodyFont.variable}`}>
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

