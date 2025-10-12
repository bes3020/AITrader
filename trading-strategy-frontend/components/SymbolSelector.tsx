"use client";

import { useState } from "react";
import { Check, Info } from "lucide-react";
import { Label } from "@/components/ui/label";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { cn } from "@/lib/utils";
import { FUTURES_SYMBOLS, type FuturesSymbol } from "@/lib/futures-symbols";

interface SymbolSelectorProps {
  /**
   * Currently selected symbol
   */
  value: string;

  /**
   * Callback when symbol is changed
   */
  onChange: (symbol: string) => void;

  /**
   * Optional disabled state
   */
  disabled?: boolean;

  /**
   * Optional className for styling
   */
  className?: string;
}

/**
 * Symbol selector component for choosing futures symbols
 * Displays all available symbols with specifications on hover
 */
export function SymbolSelector({
  value,
  onChange,
  disabled = false,
  className,
}: SymbolSelectorProps) {
  const [hoveredSymbol, setHoveredSymbol] = useState<string | null>(null);

  return (
    <div className={className}>
      <Label className="text-base font-medium mb-3 block">
        Futures Symbol
        <span className="text-destructive ml-1">*</span>
      </Label>

      <div
        className="grid grid-cols-2 sm:grid-cols-5 gap-3"
        role="radiogroup"
        aria-label="Select futures symbol"
      >
        {FUTURES_SYMBOLS.map((symbolInfo) => {
          const isSelected = value === symbolInfo.symbol;

          return (
            <Popover key={symbolInfo.symbol}>
              <PopoverTrigger asChild>
                <button
                  type="button"
                  role="radio"
                  aria-checked={isSelected}
                  aria-label={`${symbolInfo.symbol} - ${symbolInfo.name}`}
                  disabled={disabled}
                  onClick={() => onChange(symbolInfo.symbol)}
                  onMouseEnter={() => setHoveredSymbol(symbolInfo.symbol)}
                  onMouseLeave={() => setHoveredSymbol(null)}
                  className={cn(
                    "relative flex flex-col items-start p-4 rounded-lg border-2 transition-all",
                    "hover:border-primary hover:bg-primary/5 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2",
                    "disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:border-border",
                    isSelected
                      ? "border-primary bg-primary/10 shadow-sm"
                      : "border-border bg-card"
                  )}
                >
                  {/* Check icon for selected state */}
                  {isSelected && (
                    <div className="absolute top-2 right-2">
                      <Check className="h-4 w-4 text-primary" />
                    </div>
                  )}

                  {/* Symbol code */}
                  <div className="text-lg font-bold mb-1">
                    {symbolInfo.symbol}
                  </div>

                  {/* Symbol name */}
                  <div className="text-xs text-muted-foreground text-left">
                    {symbolInfo.name}
                  </div>

                  {/* Info icon */}
                  <div className="absolute bottom-2 right-2">
                    <Info className="h-3 w-3 text-muted-foreground" />
                  </div>
                </button>
              </PopoverTrigger>

              {/* Hover tooltip with specifications */}
              <PopoverContent
                side="top"
                align="center"
                className="w-80 p-4"
                onOpenAutoFocus={(e) => e.preventDefault()}
              >
                <div className="space-y-3">
                  <div>
                    <h4 className="font-semibold text-base mb-1">
                      {symbolInfo.symbol} - {symbolInfo.name}
                    </h4>
                    <p className="text-sm text-muted-foreground">
                      {symbolInfo.description}
                    </p>
                  </div>

                  <div className="border-t pt-3 space-y-2">
                    <div className="grid grid-cols-2 gap-2 text-sm">
                      <div>
                        <span className="text-muted-foreground">
                          Point Value:
                        </span>
                        <p className="font-medium">
                          ${symbolInfo.pointValue.toFixed(0)}
                        </p>
                      </div>
                      <div>
                        <span className="text-muted-foreground">
                          Tick Size:
                        </span>
                        <p className="font-medium">
                          {symbolInfo.tickSize.toFixed(2)}
                        </p>
                      </div>
                      <div>
                        <span className="text-muted-foreground">
                          Tick Value:
                        </span>
                        <p className="font-medium">
                          ${symbolInfo.tickValue.toFixed(2)}
                        </p>
                      </div>
                    </div>
                  </div>

                  <div className="border-t pt-2">
                    <p className="text-xs text-muted-foreground">
                      Hover or click the info icon to see contract
                      specifications
                    </p>
                  </div>
                </div>
              </PopoverContent>
            </Popover>
          );
        })}
      </div>

      {/* Selected symbol info bar */}
      {value && (
        <div className="mt-3 p-3 rounded-lg bg-muted/50 border">
          <div className="flex items-center justify-between">
            <div>
              <span className="text-sm font-medium">Selected: </span>
              <span className="text-sm font-bold">{value}</span>
              <span className="text-sm text-muted-foreground ml-2">
                {FUTURES_SYMBOLS.find((s) => s.symbol === value)?.name}
              </span>
            </div>
            <div className="text-xs text-muted-foreground">
              ${FUTURES_SYMBOLS.find((s) => s.symbol === value)?.pointValue}/pt
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
