import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import { ThemeSwitcher } from "@/components/ThemeSwitcher";

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
        {/* Theme Switcher - Fixed position top right */}
        <div className="fixed top-4 right-4 z-50">
          <ThemeSwitcher />
        </div>
        {children}
      </body>
    </html>
  );
}
