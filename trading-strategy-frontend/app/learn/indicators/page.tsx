"use client";

import { useState } from "react";
import Link from "next/link";
import { Search, ArrowRight, TrendingUp, Activity, BarChart3, Volume2, BookOpen } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import {
  INDICATOR_DEFINITIONS,
  getIndicatorsByCategory,
  searchIndicators,
  type IndicatorCategory,
} from "@/lib/indicator-definitions";

const CATEGORY_INFO: Record<IndicatorCategory, { icon: React.ReactNode; description: string }> = {
  trend: {
    icon: <TrendingUp className="h-5 w-5" />,
    description: "Identify market direction and trend strength",
  },
  momentum: {
    icon: <Activity className="h-5 w-5" />,
    description: "Measure the speed and magnitude of price movements",
  },
  volatility: {
    icon: <BarChart3 className="h-5 w-5" />,
    description: "Assess market volatility and price range",
  },
  volume: {
    icon: <Volume2 className="h-5 w-5" />,
    description: "Analyze trading volume and market participation",
  },
  price: {
    icon: <BarChart3 className="h-5 w-5" />,
    description: "Track price levels and movements",
  },
  other: {
    icon: <BookOpen className="h-5 w-5" />,
    description: "Additional technical analysis tools",
  },
};

export default function IndicatorsLearnPage() {
  const [searchQuery, setSearchQuery] = useState("");

  const categories: IndicatorCategory[] = ["trend", "momentum", "volatility", "volume", "price", "other"];

  const filteredIndicators = searchQuery
    ? searchIndicators(searchQuery)
    : Object.values(INDICATOR_DEFINITIONS);

  const indicatorsByCategory = searchQuery
    ? null
    : categories.reduce((acc, category) => {
        acc[category] = getIndicatorsByCategory(category);
        return acc;
      }, {} as Record<IndicatorCategory, typeof INDICATOR_DEFINITIONS[string][]>);

  return (
    <div className="min-h-screen bg-background">
      <div className="container mx-auto py-12 px-4">
        {/* Header */}
        <div className="text-center mb-12">
          <h1 className="text-4xl font-bold mb-4">Learn Technical Indicators</h1>
          <p className="text-xl text-muted-foreground max-w-2xl mx-auto">
            Master the tools professional traders use to analyze markets and make informed decisions
          </p>
        </div>

        {/* Search */}
        <div className="max-w-2xl mx-auto mb-12">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-muted-foreground" />
            <Input
              placeholder="Search indicators (e.g., RSI, MACD, Bollinger Bands)..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-10 h-12 text-lg"
            />
          </div>
        </div>

        {/* Quick Stats */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-12">
          <Card>
            <CardContent className="pt-6">
              <div className="text-center">
                <div className="text-3xl font-bold mb-2">
                  {Object.keys(INDICATOR_DEFINITIONS).length}
                </div>
                <div className="text-sm text-muted-foreground">
                  Technical Indicators
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="text-center">
                <div className="text-3xl font-bold mb-2">6</div>
                <div className="text-sm text-muted-foreground">
                  Category Groups
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="text-center">
                <div className="text-3xl font-bold mb-2">∞</div>
                <div className="text-sm text-muted-foreground">
                  Strategy Combinations
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Results */}
        {searchQuery ? (
          // Search Results
          <div>
            <h2 className="text-2xl font-bold mb-6">
              Search Results ({filteredIndicators.length})
            </h2>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {filteredIndicators.map((indicator) => (
                <Link key={indicator.id} href={`/learn/indicators/${indicator.id}`}>
                  <Card className="h-full hover:border-primary hover:shadow-lg transition-all cursor-pointer">
                    <CardHeader>
                      <div className="flex items-start justify-between mb-2">
                        <div
                          className="p-2 rounded-lg"
                          style={{ backgroundColor: `${indicator.color}20` }}
                        >
                          <div
                            className="h-5 w-5 rounded"
                            style={{ backgroundColor: indicator.color }}
                          />
                        </div>
                        <Badge
                          variant="outline"
                          style={{
                            borderColor: indicator.color,
                            color: indicator.color,
                          }}
                        >
                          {indicator.category}
                        </Badge>
                      </div>
                      <CardTitle className="text-lg">{indicator.shortName}</CardTitle>
                      <CardDescription className="line-clamp-2">
                        {indicator.description}
                      </CardDescription>
                    </CardHeader>
                    <CardContent>
                      <div className="flex items-center justify-between">
                        <div className="flex flex-wrap gap-1">
                          {indicator.commonUsage.slice(0, 2).map((usage) => (
                            <Badge key={usage} variant="secondary" className="text-xs">
                              {usage}
                            </Badge>
                          ))}
                        </div>
                        <ArrowRight className="h-4 w-4 text-muted-foreground" />
                      </div>
                    </CardContent>
                  </Card>
                </Link>
              ))}
            </div>
          </div>
        ) : (
          // Category View
          <div className="space-y-12">
            {categories.map((category) => {
              const indicators = indicatorsByCategory?.[category] || [];
              if (indicators.length === 0) return null;

              return (
                <div key={category}>
                  <div className="flex items-center gap-3 mb-6">
                    <div
                      className="p-2 rounded-lg bg-primary/10 text-primary"
                    >
                      {CATEGORY_INFO[category].icon}
                    </div>
                    <div>
                      <h2 className="text-2xl font-bold capitalize">{category} Indicators</h2>
                      <p className="text-sm text-muted-foreground">
                        {CATEGORY_INFO[category].description}
                      </p>
                    </div>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                    {indicators.map((indicator) => (
                      <Link key={indicator.id} href={`/learn/indicators/${indicator.id}`}>
                        <Card className="h-full hover:border-primary hover:shadow-lg transition-all cursor-pointer">
                          <CardHeader>
                            <div className="flex items-start justify-between mb-2">
                              <div
                                className="p-2 rounded-lg"
                                style={{ backgroundColor: `${indicator.color}20` }}
                              >
                                <div
                                  className="h-5 w-5 rounded"
                                  style={{ backgroundColor: indicator.color }}
                                />
                              </div>
                              {indicator.range && (
                                <Badge variant="outline" className="text-xs">
                                  {indicator.range.min}-{indicator.range.max}
                                </Badge>
                              )}
                            </div>
                            <CardTitle className="text-lg">{indicator.shortName}</CardTitle>
                            <CardDescription className="line-clamp-2">
                              {indicator.description}
                            </CardDescription>
                          </CardHeader>
                          <CardContent>
                            <div className="space-y-3">
                              <div className="flex flex-wrap gap-1">
                                {indicator.commonUsage.slice(0, 3).map((usage) => (
                                  <Badge key={usage} variant="secondary" className="text-xs">
                                    {usage}
                                  </Badge>
                                ))}
                              </div>
                              <div className="flex items-center justify-between text-sm text-muted-foreground">
                                <span>{indicator.examples.length} examples</span>
                                <ArrowRight className="h-4 w-4" />
                              </div>
                            </div>
                          </CardContent>
                        </Card>
                      </Link>
                    ))}
                  </div>
                </div>
              );
            })}
          </div>
        )}

        {/* Footer CTA */}
        <div className="mt-16 text-center">
          <Card className="max-w-2xl mx-auto">
            <CardHeader>
              <CardTitle>Ready to Build Your Strategy?</CardTitle>
              <CardDescription>
                Use these indicators to create powerful trading strategies
              </CardDescription>
            </CardHeader>
            <CardContent>
              <Link href="/">
                <Button size="lg" className="w-full">
                  Open Strategy Builder
                  <ArrowRight className="ml-2 h-4 w-4" />
                </Button>
              </Link>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
