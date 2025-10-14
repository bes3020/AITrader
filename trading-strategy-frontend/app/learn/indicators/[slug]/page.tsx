import { notFound } from "next/navigation";
import { ArrowLeft, BookOpen, TrendingUp, TrendingDown, Lightbulb, AlertTriangle, ExternalLink } from "lucide-react";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion";
import { INDICATOR_DEFINITIONS, getIndicator } from "@/lib/indicator-definitions";

interface IndicatorEducationPageProps {
  params: Promise<{
    slug: string;
  }>;
}

export async function generateStaticParams() {
  return Object.keys(INDICATOR_DEFINITIONS).map((id) => ({
    slug: id,
  }));
}

export async function generateMetadata({ params }: IndicatorEducationPageProps) {
  const { slug } = await params;
  const indicator = getIndicator(slug);

  if (!indicator) {
    return {
      title: "Indicator Not Found",
    };
  }

  return {
    title: `${indicator.name} - Learn Trading Indicators`,
    description: indicator.description,
  };
}

export default async function IndicatorEducationPage({ params }: IndicatorEducationPageProps) {
  const { slug } = await params;
  const indicator = getIndicator(slug);

  if (!indicator) {
    notFound();
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="container mx-auto py-8 px-4 max-w-4xl">
        {/* Header */}
        <div className="mb-6">
          <Link href="/learn/indicators">
            <Button variant="ghost" size="sm" className="mb-4">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to All Indicators
            </Button>
          </Link>

          <div className="flex items-start justify-between mb-4">
            <div>
              <h1 className="text-4xl font-bold mb-2">{indicator.name}</h1>
              <p className="text-xl text-muted-foreground">{indicator.shortName}</p>
            </div>
            <Badge
              variant="outline"
              className="text-sm"
              style={{
                borderColor: indicator.color,
                backgroundColor: `${indicator.color}20`,
                color: indicator.color,
              }}
            >
              {indicator.category.toUpperCase()}
            </Badge>
          </div>

          <p className="text-lg text-muted-foreground leading-relaxed">
            {indicator.description}
          </p>
        </div>

        <Separator className="my-8" />

        {/* Main Content */}
        <div className="space-y-8">
          {/* Formula Section */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <BookOpen className="h-5 w-5" />
                Formula
              </CardTitle>
            </CardHeader>
            <CardContent>
              <code className="text-sm bg-muted p-4 rounded-md block font-mono">
                {indicator.formula}
              </code>
            </CardContent>
          </Card>

          {/* Value Range */}
          {indicator.range && (
            <Card>
              <CardHeader>
                <CardTitle>Value Range</CardTitle>
                <CardDescription>
                  Understanding the boundaries of this indicator
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="flex items-center justify-between p-4 bg-muted rounded-lg">
                  <div className="text-center flex-1">
                    <div className="text-2xl font-bold">{indicator.range.min}</div>
                    <div className="text-sm text-muted-foreground">Minimum</div>
                  </div>
                  <div className="text-2xl text-muted-foreground">→</div>
                  <div className="text-center flex-1">
                    <div className="text-2xl font-bold">{indicator.range.max}</div>
                    <div className="text-sm text-muted-foreground">Maximum</div>
                  </div>
                </div>
              </CardContent>
            </Card>
          )}

          {/* Parameters */}
          {indicator.parameters.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>Configurable Parameters</CardTitle>
                <CardDescription>
                  Adjust these settings to fine-tune the indicator
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {indicator.parameters.map((param) => (
                    <div key={param.name} className="p-4 bg-muted rounded-lg">
                      <div className="flex items-center justify-between mb-2">
                        <h4 className="font-semibold">{param.label}</h4>
                        <Badge variant="secondary">
                          Default: {param.default}
                        </Badge>
                      </div>
                      <p className="text-sm text-muted-foreground">
                        {param.description}
                      </p>
                      {param.type === "number" && param.min !== undefined && param.max !== undefined && (
                        <p className="text-xs text-muted-foreground mt-2">
                          Range: {param.min} - {param.max}
                        </p>
                      )}
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}

          {/* Interpretations */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Lightbulb className="h-5 w-5" />
                How to Interpret Signals
              </CardTitle>
              <CardDescription>
                Understanding what the indicator is telling you
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <div className="p-4 bg-green-50 dark:bg-green-950 rounded-lg">
                  <div className="flex items-start gap-3">
                    <TrendingUp className="h-5 w-5 text-green-600 dark:text-green-400 mt-0.5 shrink-0" />
                    <div>
                      <h4 className="font-semibold text-green-900 dark:text-green-100 mb-1">
                        Bullish Signal
                      </h4>
                      <p className="text-sm text-green-700 dark:text-green-300">
                        {indicator.interpretations.bullish}
                      </p>
                    </div>
                  </div>
                </div>

                <div className="p-4 bg-gray-50 dark:bg-gray-900 rounded-lg">
                  <div className="flex items-start gap-3">
                    <div className="h-5 w-5 rounded-full bg-gray-400 mt-0.5 shrink-0" />
                    <div>
                      <h4 className="font-semibold text-gray-900 dark:text-gray-100 mb-1">
                        Neutral Signal
                      </h4>
                      <p className="text-sm text-gray-700 dark:text-gray-300">
                        {indicator.interpretations.neutral}
                      </p>
                    </div>
                  </div>
                </div>

                <div className="p-4 bg-red-50 dark:bg-red-950 rounded-lg">
                  <div className="flex items-start gap-3">
                    <TrendingDown className="h-5 w-5 text-red-600 dark:text-red-400 mt-0.5 shrink-0" />
                    <div>
                      <h4 className="font-semibold text-red-900 dark:text-red-100 mb-1">
                        Bearish Signal
                      </h4>
                      <p className="text-sm text-red-700 dark:text-red-300">
                        {indicator.interpretations.bearish}
                      </p>
                    </div>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Common Usage */}
          <Card>
            <CardHeader>
              <CardTitle>Best Used For</CardTitle>
              <CardDescription>
                Trading styles and timeframes where this indicator excels
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="flex flex-wrap gap-2">
                {indicator.commonUsage.map((usage) => (
                  <Badge key={usage} variant="secondary" className="text-sm py-2 px-4">
                    {usage}
                  </Badge>
                ))}
              </div>
            </CardContent>
          </Card>

          {/* Examples */}
          {indicator.examples.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>Practical Examples</CardTitle>
                <CardDescription>
                  Real-world trading scenarios using this indicator
                </CardDescription>
              </CardHeader>
              <CardContent>
                <Accordion type="single" collapsible className="w-full">
                  {indicator.examples.map((example, idx) => (
                    <AccordionItem key={idx} value={`example-${idx}`}>
                      <AccordionTrigger>
                        <div className="flex items-center gap-2">
                          <Badge variant="outline">Example {idx + 1}</Badge>
                          <span className="text-left">{example.description}</span>
                        </div>
                      </AccordionTrigger>
                      <AccordionContent>
                        <div className="space-y-3">
                          <div>
                            <p className="text-sm font-medium mb-2">Condition:</p>
                            <code className="text-sm bg-muted p-3 rounded block font-mono">
                              {example.condition}
                            </code>
                          </div>
                          <p className="text-sm text-muted-foreground">
                            {example.description}
                          </p>
                        </div>
                      </AccordionContent>
                    </AccordionItem>
                  ))}
                </Accordion>
              </CardContent>
            </Card>
          )}

          {/* Warnings */}
          {indicator.warnings && indicator.warnings.length > 0 && (
            <Alert>
              <AlertTriangle className="h-4 w-4" />
              <AlertDescription>
                <h4 className="font-semibold mb-2">Important Considerations</h4>
                <ul className="list-disc list-inside space-y-1">
                  {indicator.warnings.map((warning, idx) => (
                    <li key={idx} className="text-sm">
                      {warning}
                    </li>
                  ))}
                </ul>
              </AlertDescription>
            </Alert>
          )}

          {/* Chart Display Info */}
          <Card>
            <CardHeader>
              <CardTitle>Chart Display</CardTitle>
              <CardDescription>
                How this indicator appears on your charts
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="flex items-center gap-3">
                <Badge variant="outline" className="text-sm py-2 px-4">
                  {indicator.chartType === "overlay"
                    ? "📊 Overlays on price chart"
                    : indicator.chartType === "separate"
                    ? "📈 Displays in separate panel"
                    : indicator.chartType === "histogram"
                    ? "📊 Histogram display"
                    : "📉 Band overlay"}
                </Badge>
                {indicator.outputs.length > 1 && (
                  <Badge variant="secondary" className="text-sm py-2 px-4">
                    {indicator.outputs.length} output values
                  </Badge>
                )}
              </div>
              <div className="mt-4 space-y-2">
                {indicator.outputs.map((output) => (
                  <div key={output.name} className="flex items-center gap-2">
                    <div
                      className="h-3 w-3 rounded-full"
                      style={{ backgroundColor: output.color }}
                    />
                    <span className="text-sm">{output.label}</span>
                    <Badge variant="outline" className="text-xs">
                      {output.style}
                    </Badge>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>

          {/* External Resources */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <ExternalLink className="h-5 w-5" />
                Learn More
              </CardTitle>
              <CardDescription>
                Additional resources to deepen your understanding
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-2">
                <a
                  href={`https://www.investopedia.com/search?q=${encodeURIComponent(indicator.name)}`}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="flex items-center gap-2 text-sm text-primary hover:underline"
                >
                  <ExternalLink className="h-4 w-4" />
                  Investopedia: {indicator.name}
                </a>
                <a
                  href={`https://www.tradingview.com/ideas/search/?text=${encodeURIComponent(indicator.shortName)}`}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="flex items-center gap-2 text-sm text-primary hover:underline"
                >
                  <ExternalLink className="h-4 w-4" />
                  TradingView Ideas: {indicator.shortName}
                </a>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Footer CTA */}
        <div className="mt-12 text-center">
          <Link href="/">
            <Button size="lg">
              Try This Indicator in Strategy Builder
            </Button>
          </Link>
        </div>
      </div>
    </div>
  );
}
