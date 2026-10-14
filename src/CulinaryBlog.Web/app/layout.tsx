import type { Metadata } from "next";
import type { ReactNode } from "react";
import { AuthProvider } from "@/lib/auth/auth-context";
import QueryProvider from "@/lib/query/query-provider";
import "./globals.css";

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
          <AuthProvider>{children}</AuthProvider>
        </QueryProvider>
      </body>
    </html>
  );
}

