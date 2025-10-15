"use client";

import { useState, useMemo } from "react";
import { useRouter } from "next/navigation";
import { useStrategies } from "@/lib/hooks/useStrategies";
import { useTags } from "@/lib/hooks/useTags";
import { Strategy } from "@/lib/types";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { StrategyFilters } from "@/components/strategies/StrategyFilters";
import {
  Plus,
  Search,
  Star,
  Archive,
  TrendingUp,
  TrendingDown,
  Calendar,
  Filter,
  MoreVertical,
  Copy,
  Edit,
  Trash2,
  Download,
  GitBranch,
} from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

export default function StrategiesPage() {
  const router = useRouter();
  const { data: strategies = [], isLoading } = useStrategies();
  const { data: tags = [] } = useTags();

  // Filters state
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedTags, setSelectedTags] = useState<string[]>([]);
  const [selectedSymbols, setSelectedSymbols] = useState<string[]>([]);
  const [minWinRate, setMinWinRate] = useState(0);
  const [showFavorites, setShowFavorites] = useState(false);
  const [showArchived, setShowArchived] = useState(false);
  const [sortBy, setSortBy] = useState<"name" | "winRate" | "pnl" | "date">("date");

  // Apply filters
  const filteredStrategies = useMemo(() => {
    return strategies.filter((strategy: Strategy) => {
      // Search filter
      if (searchQuery) {
        const query = searchQuery.toLowerCase();
        if (
          !strategy.name.toLowerCase().includes(query) &&
          !strategy.description?.toLowerCase().includes(query)
        ) {
          return false;
        }
      }

      // Tag filter
      if (selectedTags.length > 0) {
        if (!strategy.tags || !selectedTags.some(tag => strategy.tags?.includes(tag))) {
          return false;
        }
      }

      // Symbol filter
      if (selectedSymbols.length > 0) {
        if (!strategy.symbol || !selectedSymbols.includes(strategy.symbol)) {
          return false;
        }
      }

      // Win rate filter
      if (minWinRate > 0 && strategy.results && strategy.results.length > 0) {
        const latestResult = strategy.results[0];
        if ((latestResult.winRate * 100) < minWinRate) {
          return false;
        }
      }

      // Favorites filter
      if (showFavorites && !strategy.isFavorite) {
        return false;
      }

      // Archived filter
      if (!showArchived && strategy.isArchived) {
        return false;
      }

      return true;
    });
  }, [strategies, searchQuery, selectedTags, selectedSymbols, minWinRate, showFavorites, showArchived]);

  // Apply sorting
  const sortedStrategies = useMemo(() => {
    const sorted = [...filteredStrategies];
    sorted.sort((a: Strategy, b: Strategy) => {
      switch (sortBy) {
        case "name":
          return a.name.localeCompare(b.name);
        case "winRate":
          const aWinRate = a.results?.[0]?.winRate || 0;
          const bWinRate = b.results?.[0]?.winRate || 0;
          return bWinRate - aWinRate;
        case "pnl":
          const aPnl = a.results?.[0]?.totalPnl || 0;
          const bPnl = b.results?.[0]?.totalPnl || 0;
          return bPnl - aPnl;
        case "date":
        default:
          return new Date(b.createdAt || 0).getTime() - new Date(a.createdAt || 0).getTime();
      }
    });
    return sorted;
  }, [filteredStrategies, sortBy]);

  const handleResetFilters = () => {
    setSelectedTags([]);
    setSelectedSymbols([]);
    setMinWinRate(0);
    setShowFavorites(false);
    setShowArchived(false);
    setSearchQuery("");
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-background to-muted/20 py-12 px-4">
        <div className="max-w-7xl mx-auto">
          <div className="text-center py-12">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto"></div>
            <p className="mt-4 text-muted-foreground">Loading strategies...</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-b from-background to-muted/20 py-8 px-4">
      <div className="max-w-[1800px] mx-auto">
        {/* Header */}
        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center mb-8 gap-4">
          <div>
            <h1 className="text-4xl font-bold tracking-tight">My Strategies</h1>
            <p className="text-muted-foreground mt-2">
              {sortedStrategies.length} {sortedStrategies.length === 1 ? 'strategy' : 'strategies'} found
            </p>
          </div>
          <Button onClick={() => router.push("/")} size="lg">
            <Plus className="mr-2 h-5 w-5" />
            New Strategy
          </Button>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
          {/* Left Sidebar - Filters */}
          <div className="lg:col-span-1">
            <Card className="p-6 sticky top-20">
              <StrategyFilters
                tags={tags}
                selectedTags={selectedTags}
                onTagToggle={(tag) =>
                  setSelectedTags((prev) =>
                    prev.includes(tag) ? prev.filter((t) => t !== tag) : [...prev, tag]
                  )
                }
                selectedSymbols={selectedSymbols}
                onSymbolToggle={(symbol) =>
                  setSelectedSymbols((prev) =>
                    prev.includes(symbol) ? prev.filter((s) => s !== symbol) : [...prev, symbol]
                  )
                }
                minWinRate={minWinRate}
                onMinWinRateChange={setMinWinRate}
                showFavorites={showFavorites}
                onShowFavoritesToggle={() => setShowFavorites(!showFavorites)}
                showArchived={showArchived}
                onShowArchivedToggle={() => setShowArchived(!showArchived)}
                onReset={handleResetFilters}
              />
            </Card>
          </div>

          {/* Main Content - Strategy List */}
          <div className="lg:col-span-3 space-y-6">
            {/* Search and Sort */}
            <div className="flex gap-4">
              <div className="relative flex-1">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Search strategies..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="pl-10"
                />
              </div>
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline">
                    Sort: {sortBy === 'name' ? 'Name' : sortBy === 'winRate' ? 'Win Rate' : sortBy === 'pnl' ? 'P&L' : 'Date'}
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem onClick={() => setSortBy('date')}>
                    Date (Newest First)
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => setSortBy('name')}>
                    Name (A-Z)
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => setSortBy('winRate')}>
                    Win Rate (High to Low)
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => setSortBy('pnl')}>
                    P&L (High to Low)
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>

            {/* Strategy Cards */}
            {sortedStrategies.length === 0 ? (
              <Card className="p-12 text-center">
                <Filter className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                <h3 className="text-lg font-semibold mb-2">No strategies found</h3>
                <p className="text-muted-foreground mb-4">
                  {searchQuery
                    ? "Try adjusting your search or filters"
                    : "Get started by creating your first strategy"}
                </p>
                {!searchQuery && (
                  <Button onClick={() => router.push("/")}>
                    <Plus className="mr-2 h-4 w-4" />
                    Create Strategy
                  </Button>
                )}
              </Card>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {sortedStrategies.map((strategy: Strategy) => (
                  <Card
                    key={strategy.id}
                    className="p-6 hover:shadow-lg transition-all cursor-pointer group"
                    onClick={() => router.push(`/strategies/${strategy.id}`)}
                  >
                    {/* Header */}
                    <div className="flex items-start justify-between mb-4">
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 mb-1">
                          <h3 className="font-semibold text-lg truncate">{strategy.name}</h3>
                          {strategy.isFavorite && (
                            <Star className="h-4 w-4 text-yellow-500 fill-yellow-500 flex-shrink-0" />
                          )}
                        </div>
                        <div className="flex items-center gap-2">
                          <Badge variant="outline" className="text-xs">
                            {strategy.symbol}
                          </Badge>
                          {strategy.isArchived && (
                            <Badge variant="secondary" className="text-xs">
                              <Archive className="h-3 w-3 mr-1" />
                              Archived
                            </Badge>
                          )}
                          {strategy.parentStrategyId && (
                            <Badge variant="outline" className="text-xs">
                              v{strategy.versionNumber}
                            </Badge>
                          )}
                        </div>
                      </div>

                      <DropdownMenu>
                        <DropdownMenuTrigger asChild onClick={(e) => e.stopPropagation()}>
                          <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
                            <MoreVertical className="h-4 w-4" />
                          </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end">
                          <DropdownMenuItem onClick={(e) => {
                            e.stopPropagation();
                            router.push(`/strategies/${strategy.id}/edit`);
                          }}>
                            <Edit className="h-4 w-4 mr-2" />
                            Edit
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={(e) => e.stopPropagation()}>
                            <Copy className="h-4 w-4 mr-2" />
                            Duplicate
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={(e) => e.stopPropagation()}>
                            <GitBranch className="h-4 w-4 mr-2" />
                            Create Version
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={(e) => e.stopPropagation()}>
                            <Download className="h-4 w-4 mr-2" />
                            Export
                          </DropdownMenuItem>
                          <DropdownMenuSeparator />
                          <DropdownMenuItem
                            onClick={(e) => e.stopPropagation()}
                            className="text-destructive"
                          >
                            <Trash2 className="h-4 w-4 mr-2" />
                            {strategy.isArchived ? 'Delete' : 'Archive'}
                          </DropdownMenuItem>
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </div>

                    {/* Description */}
                    <p className="text-sm text-muted-foreground mb-4 line-clamp-2">
                      {strategy.description || 'No description'}
                    </p>

                    {/* Tags */}
                    {strategy.tags && strategy.tags.length > 0 && (
                      <div className="flex flex-wrap gap-1 mb-4">
                        {strategy.tags.slice(0, 3).map((tagName, idx) => {
                          const tag = tags.find((t: any) => t.name === tagName);
                          return (
                            <Badge
                              key={idx}
                              variant="secondary"
                              className="text-xs"
                              style={{
                                backgroundColor: tag?.color + '20',
                                borderColor: tag?.color,
                              }}
                            >
                              {tagName}
                            </Badge>
                          );
                        })}
                        {strategy.tags.length > 3 && (
                          <Badge variant="secondary" className="text-xs">
                            +{strategy.tags.length - 3}
                          </Badge>
                        )}
                      </div>
                    )}

                    {/* Performance Metrics */}
                    {strategy.results && strategy.results.length > 0 && (
                      <div className="pt-4 border-t">
                        <div className="grid grid-cols-3 gap-4">
                          <div>
                            <p className="text-xs text-muted-foreground mb-1">Win Rate</p>
                            <p className="font-semibold">
                              {(strategy.results[0].winRate * 100).toFixed(1)}%
                            </p>
                          </div>
                          <div>
                            <p className="text-xs text-muted-foreground mb-1">Total P&L</p>
                            <p
                              className={`font-semibold ${
                                strategy.results[0].totalPnl >= 0
                                  ? "text-green-600"
                                  : "text-red-600"
                              }`}
                            >
                              ${strategy.results[0].totalPnl.toLocaleString()}
                            </p>
                          </div>
                          <div>
                            <p className="text-xs text-muted-foreground mb-1">Trades</p>
                            <p className="font-semibold">{strategy.results[0].totalTrades}</p>
                          </div>
                        </div>
                      </div>
                    )}

                    {/* Footer */}
                    <div className="flex items-center justify-between text-xs text-muted-foreground mt-4 pt-4 border-t">
                      <div className="flex items-center">
                        <Calendar className="h-3 w-3 mr-1" />
                        {new Date(strategy.createdAt || '').toLocaleDateString()}
                      </div>
                      {strategy.lastBacktestedAt && (
                        <div>
                          Last tested: {new Date(strategy.lastBacktestedAt).toLocaleDateString()}
                        </div>
                      )}
                    </div>
                  </Card>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
