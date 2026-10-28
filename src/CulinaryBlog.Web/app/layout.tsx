import type { Metadata } from "next";
import type { ReactNode } from "react";
import { AuthProvider } from "@/lib/auth/auth-context";
import QueryProvider from "@/lib/query/query-provider";
import { AppChrome } from "@/components/shared/app-chrome";
import "./globals.css";
import "./design-tokens.css";
import "@/components/shared/shared.css";

export const metadata: Metadata = {
  title: {
    default: "Culinary Blog",
    template: "%s | Culinary Blog",
  },
  description: "Khám phá, chia sẻ và lưu giữ những công thức đáng nhớ.",
};

export default function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <html lang="vi">
      <body>
        <QueryProvider>
          <AuthProvider>
            <AppChrome>{children}</AppChrome>
          </AuthProvider>
        </QueryProvider>
      </body>
    </html>
  );
}

