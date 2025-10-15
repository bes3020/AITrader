"use client";

import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { InfoIcon } from "lucide-react";

interface FormulaEditorProps {
  value: string;
  onChange: (value: string) => void;
  error?: string;
}

export function FormulaEditor({ value, onChange, error }: FormulaEditorProps) {
  return (
    <div className="space-y-3">
      <div>
        <Label htmlFor="formula">
          Formula
          <span className="text-destructive ml-1">*</span>
        </Label>
        <p className="text-xs text-muted-foreground mt-1 mb-2">
          Write C# code that returns a decimal array. Available variables: Close, Open, High, Low, Volume (all decimal arrays).
        </p>
        <Textarea
          id="formula"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          className="font-mono text-sm min-h-[200px]"
          placeholder={`// Example: Custom momentum indicator
var momentum = new decimal[Close.Length];
for (int i = period; i < Close.Length; i++)
{
    momentum[i] = Close[i] - Close[i - period];
}
return momentum;`}
          required
        />
        {error && (
          <p className="text-sm text-destructive mt-1">{error}</p>
        )}
      </div>

      <Alert>
        <InfoIcon className="h-4 w-4" />
        <AlertDescription className="text-xs">
          <strong>Tips:</strong>
          <ul className="list-disc list-inside mt-1 space-y-1">
            <li>Access price data: Close, Open, High, Low, Volume</li>
            <li>Use parameters from the Parameters section</li>
            <li>Return a decimal array with same length as input</li>
            <li>Handle edge cases (e.g., first N periods)</li>
            <li>Formula is compiled and validated before saving</li>
          </ul>
        </AlertDescription>
      </Alert>

      <Alert>
        <InfoIcon className="h-4 w-4" />
        <AlertDescription className="text-xs">
          <strong>Example formulas:</strong>
          <div className="mt-2 space-y-2">
            <div>
              <p className="font-medium">Price Change:</p>
              <code className="text-xs bg-muted p-1 rounded block mt-1">
                return Close.Select((c, i) =&gt; i &gt; 0 ? c - Close[i-1] : 0m).ToArray();
              </code>
            </div>
            <div>
              <p className="font-medium">Smoothed Price:</p>
              <code className="text-xs bg-muted p-1 rounded block mt-1">
                var alpha = 2m / (period + 1);{"\n"}
                var ema = new decimal[Close.Length];{"\n"}
                ema[0] = Close[0];{"\n"}
                for (int i = 1; i &lt; Close.Length; i++){"\n"}
                {"  "}ema[i] = alpha * Close[i] + (1 - alpha) * ema[i-1];{"\n"}
                return ema;
              </code>
            </div>
          </div>
        </AlertDescription>
      </Alert>
    </div>
  );
}
