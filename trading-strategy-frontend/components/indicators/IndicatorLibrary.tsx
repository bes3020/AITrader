"use client";

import { useState, useMemo } from "react";
import { Search, Star, Clock, ChevronDown, ChevronRight, Info } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import { IndicatorQuickTooltip } from "@/components/indicators/IndicatorTooltip";
import {
  INDICATOR_DEFINITIONS,
  getIndicatorsByCategory,
  searchIndicators,
  getRecentIndicators,
  getFavoriteIndicators,
  toggleFavoriteIndicator,
  isIndicatorFavorited,
  addRecentIndicator,
  CATEGORY_COLORS,
  type IndicatorDefinition,
  type IndicatorCategory,
} from "@/lib/indicator-definitions";

interface IndicatorLibraryProps {
  onSelectIndicator: (indicator: IndicatorDefinition) => void;
  selectedIndicatorId?: string;
}

const CATEGORY_LABELS: Record<IndicatorCategory, string> = {
  trend: "Trend",
  momentum: "Momentum",
  volatility: "Volatility",
  volume: "Volume",
};

const CATEGORY_ICONS: Record<IndicatorCategory, string> = {
  trend: "TrendingUp",
  momentum: "Activity",
  volatility: "Zap",
  volume: "BarChart3",
};

export function IndicatorLibrary({
  onSelectIndicator,
  selectedIndicatorId,
}: IndicatorLibraryProps) {
  const [searchQuery, setSearchQuery] = useState("");
  const [expandedCategories, setExpandedCategories] = useState<
    Set<IndicatorCategory | "recent" | "favorites">
  >(new Set(["trend", "momentum", "volatility", "volume"]));
  const [favorites, setFavorites] = useState<IndicatorDefinition[]>([]);
  const [recent, setRecent] = useState<IndicatorDefinition[]>([]);

  // Load favorites and recent on mount
  useMemo(() => {
    if (typeof window !== "undefined") {
      setFavorites(getFavoriteIndicators());
      setRecent(getRecentIndicators());
    }
  }, []);

  // Filter indicators based on search
  const filteredIndicators = useMemo(() => {
    if (!searchQuery.trim()) {
      return null; // Show categories
    }
    return searchIndicators(searchQuery);
  }, [searchQuery]);

  const toggleCategory = (
    category: IndicatorCategory | "recent" | "favorites"
  ) => {
    const newExpanded = new Set(expandedCategories);
    if (newExpanded.has(category)) {
      newExpanded.delete(category);
    } else {
      newExpanded.add(category);
    }
    setExpandedCategories(newExpanded);
  };

  const handleSelectIndicator = (indicator: IndicatorDefinition) => {
    addRecentIndicator(indicator.id);
    setRecent(getRecentIndicators());
    onSelectIndicator(indicator);
  };

  const handleToggleFavorite = (
    e: React.MouseEvent,
    indicatorId: string
  ) => {
    e.stopPropagation();
    toggleFavoriteIndicator(indicatorId);
    setFavorites(getFavoriteIndicators());
  };

  const renderIndicatorCard = (indicator: IndicatorDefinition) => {
    const isFavorite = isIndicatorFavorited(indicator.id);
    const isSelected = selectedIndicatorId === indicator.id;

    return (
      <Card
        key={indicator.id}
        className={`cursor-pointer transition-all hover:border-primary hover:shadow-sm ${
          isSelected ? "border-primary bg-primary/5" : ""
        }`}
        onClick={() => handleSelectIndicator(indicator)}
      >
        <CardContent className="p-4">
          <div className="flex items-start justify-between gap-3">
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-2 mb-1">
                <h4 className="font-medium text-sm truncate">
                  {indicator.shortName}
                </h4>
                <IndicatorQuickTooltip indicator={indicator} />
                <Badge
                  variant="outline"
                  className="text-xs"
                  style={{
                    borderColor: indicator.color,
                    color: indicator.color,
                  }}
                >
                  {CATEGORY_LABELS[indicator.category]}
                </Badge>
              </div>
              <p className="text-xs text-muted-foreground line-clamp-2">
                {indicator.description}
              </p>
              {indicator.range && (
                <p className="text-xs text-muted-foreground mt-1">
                  Range: {indicator.range.min} - {indicator.range.max}
                </p>
              )}
            </div>
            <Button
              variant="ghost"
              size="sm"
              className="h-8 w-8 p-0 shrink-0"
              onClick={(e) => handleToggleFavorite(e, indicator.id)}
            >
              <Star
                className={`h-4 w-4 ${
                  isFavorite ? "fill-yellow-400 text-yellow-400" : ""
                }`}
              />
            </Button>
          </div>
        </CardContent>
      </Card>
    );
  };

  const renderCategorySection = (
    category: IndicatorCategory | "recent" | "favorites",
    indicators: IndicatorDefinition[],
    label: string,
    icon: React.ReactNode
  ) => {
    if (indicators.length === 0) return null;

    const isExpanded = expandedCategories.has(category);

    return (
      <div key={category} className="mb-4">
        <Button
          variant="ghost"
          className="w-full justify-start text-left mb-2 hover:bg-muted"
          onClick={() => toggleCategory(category)}
        >
          {isExpanded ? (
            <ChevronDown className="h-4 w-4 mr-2" />
          ) : (
            <ChevronRight className="h-4 w-4 mr-2" />
          )}
          {icon}
          <span className="font-semibold">{label}</span>
          <Badge variant="secondary" className="ml-auto">
            {indicators.length}
          </Badge>
        </Button>

        {isExpanded && (
          <div className="space-y-2 pl-2">{indicators.map(renderIndicatorCard)}</div>
        )}
      </div>
    );
  };

  return (
    <div className="h-full flex flex-col">
      {/* Search */}
      <div className="p-4 border-b">
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search indicators..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="pl-9"
          />
        </div>
      </div>

      {/* Indicator List */}
      <div className="flex-1 overflow-y-auto p-4">
        {filteredIndicators ? (
          // Search Results
          <div className="space-y-2">
            {filteredIndicators.length === 0 ? (
              <div className="text-center py-8 text-muted-foreground">
                <p>No indicators found</p>
                <p className="text-sm mt-1">Try a different search term</p>
              </div>
            ) : (
              filteredIndicators.map(renderIndicatorCard)
            )}
          </div>
        ) : (
          // Category View
          <>
            {/* Favorites */}
            {favorites.length > 0 &&
              renderCategorySection(
                "favorites",
                favorites,
                "Favorites",
                <Star className="h-4 w-4 mr-2 fill-yellow-400 text-yellow-400" />
              )}

            {/* Recently Used */}
            {recent.length > 0 &&
              renderCategorySection(
                "recent",
                recent,
                "Recently Used",
                <Clock className="h-4 w-4 mr-2" />
              )}

            {/* Categories */}
            {(["trend", "momentum", "volatility", "volume"] as IndicatorCategory[]).map(
              (category) => {
                const indicators = getIndicatorsByCategory(category);
                return renderCategorySection(
                  category,
                  indicators,
                  CATEGORY_LABELS[category],
                  <div
                    className="h-3 w-3 rounded-full mr-2"
                    style={{ backgroundColor: CATEGORY_COLORS[category] }}
                  />
                );
              }
            )}
          </>
        )}
      </div>

      {/* Footer */}
      <div className="p-4 border-t bg-muted/30">
        <p className="text-xs text-muted-foreground text-center">
          {Object.keys(INDICATOR_DEFINITIONS).length} indicators available
        </p>
      </div>
    </div>
  );
}
