import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import { ThemeSwitcher } from "@/components/ThemeSwitcher";
import { Navigation } from "@/components/Navigation";
import { QueryProvider } from "@/components/providers/QueryProvider";

const inter = Inter({ subsets: ["latin"] });

export const metadata: Metadata = {
  title: "Trading Strategy Analyzer",
  description: "Analyze and backtest trading strategies",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body className={inter.className}>
        <QueryProvider>
          {/* Theme Switcher - Fixed position top right */}
          <div className="fixed top-4 right-4 z-50">
            <ThemeSwitcher />
          </div>
          {/* Navigation */}
          <Navigation />
          {children}
        </QueryProvider>
      </body>
    </html>
  );
}
