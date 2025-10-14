"use client";

import {
  Brain,
  CheckCircle2,
  XCircle,
  TrendingUp,
  Clock,
  Calendar,
  Activity,
  Lightbulb,
  BookOpen,
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import type { TradeAnalysis } from "@/lib/types";

interface TradeAnalysisPanelProps {
  /**
   * Trade analysis data with AI insights
   */
  analysis: TradeAnalysis;

  /**
   * Optional className for styling
   */
  className?: string;
}

/**
 * Displays AI-generated analysis and insights for a trade
 */
export function TradeAnalysisPanel({
  analysis,
  className,
}: TradeAnalysisPanelProps) {
  const getMarketConditionColor = (condition: string) => {
    switch (condition.toLowerCase()) {
      case "trending":
        return "bg-blue-500/10 text-blue-700 border-blue-500/20";
      case "ranging":
        return "bg-purple-500/10 text-purple-700 border-purple-500/20";
      case "volatile":
        return "bg-orange-500/10 text-orange-700 border-orange-500/20";
      case "quiet":
        return "bg-gray-500/10 text-gray-700 border-gray-500/20";
      default:
        return "bg-muted text-muted-foreground border-border";
    }
  };

  const getTimeOfDayColor = (timeOfDay: string) => {
    switch (timeOfDay.toLowerCase()) {
      case "morning":
        return "bg-amber-500/10 text-amber-700 border-amber-500/20";
      case "midday":
        return "bg-yellow-500/10 text-yellow-700 border-yellow-500/20";
      case "afternoon":
        return "bg-orange-500/10 text-orange-700 border-orange-500/20";
      case "close":
        return "bg-indigo-500/10 text-indigo-700 border-indigo-500/20";
      default:
        return "bg-muted text-muted-foreground border-border";
    }
  };

  return (
    <div className={`space-y-6 ${className}`}>
      {/* Context Information */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-lg">
            <Activity className="h-5 w-5" />
            Trade Context
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* Market Conditions */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-2">
              <p className="text-sm font-medium text-muted-foreground">
                Market Condition
              </p>
              <Badge
                variant="outline"
                className={getMarketConditionColor(analysis.marketCondition)}
              >
                {analysis.marketCondition.toUpperCase()}
              </Badge>
            </div>

            <div className="space-y-2">
              <p className="text-sm font-medium text-muted-foreground">
                Time of Day
              </p>
              <Badge
                variant="outline"
                className={getTimeOfDayColor(analysis.timeOfDay)}
              >
                <Clock className="mr-1 h-3 w-3" />
                {analysis.timeOfDay.toUpperCase()}
              </Badge>
            </div>
          </div>

          {/* Day of Week */}
          <div className="space-y-2">
            <p className="text-sm font-medium text-muted-foreground">
              Day of Week
            </p>
            <div className="flex items-center gap-2">
              <Calendar className="h-4 w-4 text-muted-foreground" />
              <span className="text-sm font-semibold">
                {analysis.dayOfWeek}
              </span>
            </div>
          </div>

          {/* Technical Indicators */}
          {(analysis.adxValue || analysis.atrValue || analysis.vixLevel) && (
            <div className="pt-4 border-t space-y-3">
              <p className="text-sm font-medium text-muted-foreground">
                Technical Indicators
              </p>
              <div className="grid grid-cols-3 gap-4">
                {analysis.adxValue && (
                  <div className="space-y-1">
                    <p className="text-xs text-muted-foreground">ADX</p>
                    <p className="text-lg font-bold">
                      {analysis.adxValue.toFixed(1)}
                    </p>
                  </div>
                )}
                {analysis.atrValue && (
                  <div className="space-y-1">
                    <p className="text-xs text-muted-foreground">ATR</p>
                    <p className="text-lg font-bold">
                      {analysis.atrValue.toFixed(2)}
                    </p>
                  </div>
                )}
                {analysis.vixLevel && (
                  <div className="space-y-1">
                    <p className="text-xs text-muted-foreground">VIX</p>
                    <p className="text-lg font-bold">
                      {analysis.vixLevel.toFixed(1)}
                    </p>
                  </div>
                )}
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Entry and Exit Reasons */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {/* Entry Reason */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <TrendingUp className="h-4 w-4 text-primary" />
              Entry Reason
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm leading-relaxed">{analysis.entryReason}</p>
          </CardContent>
        </Card>

        {/* Exit Reason */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <CheckCircle2 className="h-4 w-4 text-muted-foreground" />
              Exit Reason
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm leading-relaxed">{analysis.exitReason}</p>
          </CardContent>
        </Card>
      </div>

      {/* AI Narrative */}
      {analysis.narrative && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <Brain className="h-5 w-5 text-primary" />
              AI Narrative
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="prose prose-sm max-w-none">
              <p className="text-sm leading-relaxed whitespace-pre-wrap">
                {analysis.narrative}
              </p>
            </div>
          </CardContent>
        </Card>
      )}

      {/* What Went Right / Wrong */}
      {(analysis.whatWentRight || analysis.whatWentWrong) && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {/* What Went Right */}
          {analysis.whatWentRight && (
            <Card className="border-green-500/20">
              <CardHeader>
                <CardTitle className="flex items-center gap-2 text-base text-profit">
                  <CheckCircle2 className="h-4 w-4" />
                  What Went Right
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="prose prose-sm max-w-none">
                  <p className="text-sm leading-relaxed whitespace-pre-wrap">
                    {analysis.whatWentRight}
                  </p>
                </div>
              </CardContent>
            </Card>
          )}

          {/* What Went Wrong */}
          {analysis.whatWentWrong && (
            <Card className="border-red-500/20">
              <CardHeader>
                <CardTitle className="flex items-center gap-2 text-base text-loss">
                  <XCircle className="h-4 w-4" />
                  What Went Wrong
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="prose prose-sm max-w-none">
                  <p className="text-sm leading-relaxed whitespace-pre-wrap">
                    {analysis.whatWentWrong}
                  </p>
                </div>
              </CardContent>
            </Card>
          )}
        </div>
      )}

      {/* Lessons Learned */}
      {analysis.lessonsLearned && (
        <Card className="border-blue-500/20 bg-blue-500/5">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg text-blue-700">
              <Lightbulb className="h-5 w-5" />
              Key Lessons
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="prose prose-sm max-w-none">
              <div className="space-y-2">
                {analysis.lessonsLearned.split("\n").map((lesson, idx) => {
                  const trimmedLesson = lesson.trim();
                  if (!trimmedLesson) return null;

                  return (
                    <div key={idx} className="flex items-start gap-2">
                      <BookOpen className="h-4 w-4 text-blue-600 mt-0.5 flex-shrink-0" />
                      <p className="text-sm leading-relaxed flex-1">
                        {trimmedLesson}
                      </p>
                    </div>
                  );
                })}
              </div>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
