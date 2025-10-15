"use client";

import { Card } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Switch } from "@/components/ui/switch";
import { Plus, Edit, Trash2, Globe, Lock, Code } from "lucide-react";
import type { CustomIndicator } from "@/lib/types/indicator";
import { CATEGORY_COLORS } from "@/lib/types/indicator";
import { useState } from "react";

interface CustomIndicatorCardProps {
  indicator: CustomIndicator;
  onEdit: (indicator: CustomIndicator) => void;
  onDelete: (id: number) => void;
  onTogglePublic: (id: number, isPublic: boolean) => void;
  onUse: (indicator: CustomIndicator) => void;
}

export function CustomIndicatorCard({
  indicator,
  onEdit,
  onDelete,
  onTogglePublic,
  onUse,
}: CustomIndicatorCardProps) {
  const [isPublic, setIsPublic] = useState(indicator.isPublic);

  const handleTogglePublic = async () => {
    const newValue = !isPublic;
    setIsPublic(newValue);
    onTogglePublic(indicator.id, newValue);
  };

  // Parse parameters to display
  let parsedParams: Record<string, any> = {};
  try {
    parsedParams = JSON.parse(indicator.parameters);
  } catch {
    parsedParams = {};
  }

  const isCustomFormula = indicator.type === "Custom";

  return (
    <Card className="p-4 hover:shadow-md transition-shadow">
      {/* Header */}
      <div className="flex items-start justify-between mb-3">
        <div className="flex items-center gap-3">
          <div
            className="p-2 rounded-lg bg-primary/10"
          >
            <Code className="h-5 w-5 text-primary" />
          </div>
          <div>
            <h3 className="font-semibold">{indicator.displayName}</h3>
            <div className="flex items-center gap-2 mt-1">
              <Badge variant="outline" className="text-xs">
                {indicator.type}
              </Badge>
              {isPublic ? (
                <Globe className="h-3 w-3 text-muted-foreground" />
              ) : (
                <Lock className="h-3 w-3 text-muted-foreground" />
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Description */}
      {indicator.description && (
        <p className="text-sm text-muted-foreground mb-3 line-clamp-2">
          {indicator.description}
        </p>
      )}

      {/* Parameters */}
      <div className="mb-3">
        <p className="text-xs font-medium text-muted-foreground mb-1">Parameters:</p>
        <div className="flex flex-wrap gap-1">
          {Object.entries(parsedParams).map(([key, value]) => (
            <Badge key={key} variant="secondary" className="text-xs">
              {key}: {String(value)}
            </Badge>
          ))}
        </div>
      </div>

      {/* Formula Preview (for custom type) */}
      {isCustomFormula && indicator.formula && (
        <div className="mb-3">
          <p className="text-xs font-medium text-muted-foreground mb-1">Formula:</p>
          <code className="text-xs bg-muted p-2 rounded block overflow-x-auto whitespace-pre">
            {indicator.formula.slice(0, 100)}
            {indicator.formula.length > 100 && "..."}
          </code>
        </div>
      )}

      {/* Timestamps */}
      <div className="mb-3 text-xs text-muted-foreground">
        <p>Created: {new Date(indicator.createdAt).toLocaleDateString()}</p>
        {indicator.updatedAt !== indicator.createdAt && (
          <p>Updated: {new Date(indicator.updatedAt).toLocaleDateString()}</p>
        )}
      </div>

      {/* Public/Private Toggle */}
      <div className="flex items-center justify-between mb-3 p-2 bg-muted/50 rounded">
        <label htmlFor={`public-${indicator.id}`} className="text-sm font-medium">
          {isPublic ? "Public" : "Private"}
        </label>
        <Switch
          id={`public-${indicator.id}`}
          checked={isPublic}
          onCheckedChange={handleTogglePublic}
        />
      </div>

      {/* Actions */}
      <div className="flex gap-2">
        <Button
          variant="outline"
          size="sm"
          className="flex-1"
          onClick={() => onEdit(indicator)}
        >
          <Edit className="h-4 w-4 mr-1" />
          Edit
        </Button>
        <Button
          variant="destructive"
          size="sm"
          onClick={() => onDelete(indicator.id)}
        >
          <Trash2 className="h-4 w-4" />
        </Button>
        <Button
          variant="default"
          size="sm"
          className="flex-1"
          onClick={() => onUse(indicator)}
        >
          <Plus className="h-4 w-4 mr-1" />
          Use
        </Button>
      </div>
    </Card>
  );
}
