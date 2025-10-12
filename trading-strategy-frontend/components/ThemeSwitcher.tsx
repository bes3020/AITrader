"use client";

import { useState, useEffect } from "react";
import { Palette, Check } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { themes, type Theme } from "@/lib/themes";

/**
 * Theme switcher component
 * Allows users to select from different color themes
 */
export function ThemeSwitcher() {
  const [currentTheme, setCurrentTheme] = useState<string>("default");
  const [mounted, setMounted] = useState(false);

  // Only render after mount to avoid hydration mismatch
  useEffect(() => {
    setMounted(true);
    const saved = localStorage.getItem("theme");
    if (saved) {
      setCurrentTheme(saved);
      applyTheme(themes.find((t) => t.name === saved) || themes[0]);
    }
  }, []);

  const applyTheme = (theme: Theme) => {
    const root = document.documentElement;
    const isDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
    const colors = isDark ? theme.dark : theme.light;

    // Apply all CSS variables
    Object.entries(colors).forEach(([key, value]) => {
      const cssVar = `--color-${key.replace(/([A-Z])/g, "-$1").toLowerCase()}`;
      root.style.setProperty(cssVar, value);
    });

    setCurrentTheme(theme.name);
    localStorage.setItem("theme", theme.name);
  };

  if (!mounted) {
    return null;
  }

  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          size="icon"
          className="h-9 w-9"
          aria-label="Change theme"
        >
          <Palette className="h-4 w-4" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-80" align="end">
        <div className="space-y-3">
          <div>
            <h4 className="font-semibold text-sm mb-1">Color Theme</h4>
            <p className="text-xs text-muted-foreground">
              Select a color scheme for the application
            </p>
          </div>

          <div className="space-y-2">
            {themes.map((theme) => (
              <button
                key={theme.name}
                onClick={() => applyTheme(theme)}
                className={`w-full flex items-start gap-3 p-3 rounded-lg border-2 transition-all hover:border-primary ${
                  currentTheme === theme.name
                    ? "border-primary bg-primary/5"
                    : "border-border"
                }`}
              >
                {/* Color preview */}
                <div className="flex gap-1 mt-1">
                  <div
                    className="w-4 h-4 rounded border"
                    style={{
                      backgroundColor: theme.light.primary,
                    }}
                  />
                  <div
                    className="w-4 h-4 rounded border"
                    style={{
                      backgroundColor: theme.light.profit,
                    }}
                  />
                  <div
                    className="w-4 h-4 rounded border"
                    style={{
                      backgroundColor: theme.light.loss,
                    }}
                  />
                </div>

                {/* Theme info */}
                <div className="flex-1 text-left">
                  <div className="flex items-center justify-between">
                    <p className="font-medium text-sm">{theme.label}</p>
                    {currentTheme === theme.name && (
                      <Check className="h-4 w-4 text-primary" />
                    )}
                  </div>
                  <p className="text-xs text-muted-foreground mt-0.5">
                    {theme.description}
                  </p>
                </div>
              </button>
            ))}
          </div>

          <div className="border-t pt-2">
            <p className="text-xs text-muted-foreground">
              Theme adapts automatically to light/dark mode based on system
              preferences
            </p>
          </div>
        </div>
      </PopoverContent>
    </Popover>
  );
}
