"use client";

import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Slider } from "@/components/ui/slider";
import type { IndicatorParameter } from "@/lib/types/indicator";

interface IndicatorParameterInputProps {
  parameter: IndicatorParameter;
  value: string | number | boolean;
  onChange: (value: string | number | boolean) => void;
}

export function IndicatorParameterInput({
  parameter,
  value,
  onChange,
}: IndicatorParameterInputProps) {
  const { name, type, minValue, maxValue, description, required } = parameter;

  // Boolean type - Switch
  if (type === "bool") {
    return (
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <Label htmlFor={name}>
            {name}
            {required && <span className="text-destructive ml-1">*</span>}
          </Label>
          <Switch
            id={name}
            checked={Boolean(value)}
            onCheckedChange={(checked) => onChange(checked)}
          />
        </div>
        {description && (
          <p className="text-xs text-muted-foreground">{description}</p>
        )}
      </div>
    );
  }

  // Number types with range - Slider
  if ((type === "int" || type === "decimal") && minValue !== undefined && maxValue !== undefined) {
    const numValue = typeof value === "number" ? value : parseFloat(String(value));
    return (
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <Label htmlFor={name}>
            {name}
            {required && <span className="text-destructive ml-1">*</span>}
          </Label>
          <span className="text-sm font-medium">{numValue}</span>
        </div>
        <Slider
          id={name}
          min={minValue}
          max={maxValue}
          step={type === "int" ? 1 : 0.1}
          value={[numValue]}
          onValueChange={([newValue]) => onChange(newValue)}
        />
        {description && (
          <p className="text-xs text-muted-foreground">{description}</p>
        )}
      </div>
    );
  }

  // Integer type - Number input
  if (type === "int") {
    return (
      <div className="space-y-2">
        <Label htmlFor={name}>
          {name}
          {required && <span className="text-destructive ml-1">*</span>}
        </Label>
        <Input
          id={name}
          type="number"
          step="1"
          min={minValue}
          max={maxValue}
          value={value}
          onChange={(e) => onChange(parseInt(e.target.value, 10) || 0)}
          required={required}
        />
        {description && (
          <p className="text-xs text-muted-foreground">{description}</p>
        )}
      </div>
    );
  }

  // Decimal type - Number input with decimals
  if (type === "decimal") {
    return (
      <div className="space-y-2">
        <Label htmlFor={name}>
          {name}
          {required && <span className="text-destructive ml-1">*</span>}
        </Label>
        <Input
          id={name}
          type="number"
          step="0.01"
          min={minValue}
          max={maxValue}
          value={value}
          onChange={(e) => onChange(parseFloat(e.target.value) || 0)}
          required={required}
        />
        {description && (
          <p className="text-xs text-muted-foreground">{description}</p>
        )}
      </div>
    );
  }

  // String type - Text input
  return (
    <div className="space-y-2">
      <Label htmlFor={name}>
        {name}
        {required && <span className="text-destructive ml-1">*</span>}
      </Label>
      <Input
        id={name}
        type="text"
        value={String(value)}
        onChange={(e) => onChange(e.target.value)}
        required={required}
      />
      {description && (
        <p className="text-xs text-muted-foreground">{description}</p>
      )}
    </div>
  );
}
