"use client";

import { useState } from "react";
import { Input } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import { Slider } from "@/components/ui/slider";
import { Separator } from "@/components/ui/separator";
import { Search, X, Star, TrendingUp } from "lucide-react";
import { Button } from "@/components/ui/button";

interface StrategyFiltersProps {
  tags: any[];
  selectedTags: string[];
  onTagToggle: (tag: string) => void;
  selectedSymbols: string[];
  onSymbolToggle: (symbol: string) => void;
  minWinRate: number;
  onMinWinRateChange: (value: number) => void;
  showFavorites: boolean;
  onShowFavoritesToggle: () => void;
  showArchived: boolean;
  onShowArchivedToggle: () => void;
  onReset: () => void;
}

const SYMBOLS = ["ES", "NQ", "YM", "BTC", "CL"];

export function StrategyFilters({
  tags,
  selectedTags,
  onTagToggle,
  selectedSymbols,
  onSymbolToggle,
  minWinRate,
  onMinWinRateChange,
  showFavorites,
  onShowFavoritesToggle,
  showArchived,
  onShowArchivedToggle,
  onReset,
}: StrategyFiltersProps) {
  const hasActiveFilters =
    selectedTags.length > 0 ||
    selectedSymbols.length > 0 ||
    minWinRate > 0 ||
    showFavorites ||
    showArchived;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <h3 className="font-semibold">Filters</h3>
        {hasActiveFilters && (
          <Button variant="ghost" size="sm" onClick={onReset}>
            <X className="h-4 w-4 mr-1" />
            Clear
          </Button>
        )}
      </div>

      {/* Quick Filters */}
      <div className="space-y-3">
        <Label className="text-sm font-medium">Quick Filters</Label>
        <div className="space-y-2">
          <label className="flex items-center space-x-2 cursor-pointer">
            <Checkbox
              checked={showFavorites}
              onCheckedChange={onShowFavoritesToggle}
            />
            <Star className="h-4 w-4 text-yellow-500" />
            <span className="text-sm">Favorites Only</span>
          </label>
          <label className="flex items-center space-x-2 cursor-pointer">
            <Checkbox
              checked={showArchived}
              onCheckedChange={onShowArchivedToggle}
            />
            <span className="text-sm">Include Archived</span>
          </label>
        </div>
      </div>

      <Separator />

      {/* Symbols */}
      <div className="space-y-3">
        <Label className="text-sm font-medium">Symbols</Label>
        <div className="flex flex-wrap gap-2">
          {SYMBOLS.map((symbol) => (
            <Badge
              key={symbol}
              variant={selectedSymbols.includes(symbol) ? "default" : "outline"}
              className="cursor-pointer"
              onClick={() => onSymbolToggle(symbol)}
            >
              {symbol}
            </Badge>
          ))}
        </div>
      </div>

      <Separator />

      {/* Tags */}
      {tags.length > 0 && (
        <>
          <div className="space-y-3">
            <Label className="text-sm font-medium">Tags</Label>
            <div className="flex flex-wrap gap-2">
              {tags.map((tag) => (
                <Badge
                  key={tag.id}
                  variant={selectedTags.includes(tag.name) ? "default" : "outline"}
                  className="cursor-pointer"
                  style={{
                    backgroundColor: selectedTags.includes(tag.name)
                      ? tag.color
                      : undefined,
                    borderColor: tag.color,
                  }}
                  onClick={() => onTagToggle(tag.name)}
                >
                  {tag.name}
                </Badge>
              ))}
            </div>
          </div>
          <Separator />
        </>
      )}

      {/* Win Rate Filter */}
      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <Label className="text-sm font-medium">Min Win Rate</Label>
          <span className="text-sm text-muted-foreground">{minWinRate}%</span>
        </div>
        <Slider
          value={[minWinRate]}
          onValueChange={(values) => onMinWinRateChange(values[0])}
          max={100}
          step={5}
          className="w-full"
        />
      </div>
    </div>
  );
}
