"use client";

import { useState, useEffect } from "react";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Switch } from "@/components/ui/switch";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Loader2 } from "lucide-react";
import { FormulaEditor } from "./FormulaEditor";
import { IndicatorParameterInput } from "./IndicatorParameterInput";
import type { CustomIndicator, UpdateIndicatorRequest, BuiltInIndicator } from "@/lib/types/indicator";

interface EditIndicatorDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  indicator: CustomIndicator | null;
  onUpdate: (id: number, data: UpdateIndicatorRequest) => Promise<void>;
  builtInIndicators: BuiltInIndicator[];
}

export function EditIndicatorDialog({
  open,
  onOpenChange,
  indicator,
  onUpdate,
  builtInIndicators,
}: EditIndicatorDialogProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Form state
  const [displayName, setDisplayName] = useState("");
  const [description, setDescription] = useState("");
  const [isPublic, setIsPublic] = useState(false);
  const [formula, setFormula] = useState("");
  const [parameters, setParameters] = useState<Record<string, any>>({});

  // Update form state when indicator changes
  useEffect(() => {
    if (indicator) {
      setDisplayName(indicator.displayName);
      setDescription(indicator.description || "");
      setIsPublic(indicator.isPublic);
      setFormula(indicator.formula || "");

      try {
        setParameters(JSON.parse(indicator.parameters));
      } catch {
        setParameters({});
      }
    }
  }, [indicator]);

  if (!indicator) return null;

  const selectedBuiltIn = indicator.type !== "Custom"
    ? builtInIndicators.find(i => i.type === indicator.type)
    : null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      const data: UpdateIndicatorRequest = {
        displayName: displayName.trim(),
        description: description.trim() || undefined,
        isPublic,
        parameters: JSON.stringify(parameters),
        formula: indicator.type === "Custom" ? formula.trim() : undefined,
      };

      await onUpdate(indicator.id, data);
      onOpenChange(false);
    } catch (err: any) {
      setError(err.message || "Failed to update indicator");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Edit {indicator.displayName}</DialogTitle>
          <DialogDescription>
            Update your custom indicator settings
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          {error && (
            <Alert variant="destructive">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          <Tabs defaultValue="basic" className="w-full">
            <TabsList className="grid w-full grid-cols-2">
              <TabsTrigger value="basic">Basic Info</TabsTrigger>
              <TabsTrigger value="advanced">
                {indicator.type === "Custom" ? "Formula" : "Parameters"}
              </TabsTrigger>
            </TabsList>

            <TabsContent value="basic" className="space-y-4">
              {/* Type (read-only) */}
              <div className="space-y-2">
                <Label>Indicator Type</Label>
                <Input value={indicator.type} disabled />
                <p className="text-xs text-muted-foreground">
                  Type cannot be changed after creation
                </p>
              </div>

              {/* Name (read-only) */}
              <div className="space-y-2">
                <Label>Unique Name</Label>
                <Input value={indicator.name} disabled />
                <p className="text-xs text-muted-foreground">
                  Name cannot be changed after creation
                </p>
              </div>

              {/* Display Name */}
              <div className="space-y-2">
                <Label htmlFor="displayName">
                  Display Name
                  <span className="text-destructive ml-1">*</span>
                </Label>
                <Input
                  id="displayName"
                  value={displayName}
                  onChange={(e) => setDisplayName(e.target.value)}
                  placeholder="e.g., My EMA 20"
                  required
                />
              </div>

              {/* Description */}
              <div className="space-y-2">
                <Label htmlFor="description">Description</Label>
                <Textarea
                  id="description"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="What does this indicator measure?"
                  rows={3}
                />
              </div>

              {/* Public Toggle */}
              <div className="flex items-center justify-between p-3 bg-muted rounded-lg">
                <div>
                  <Label htmlFor="isPublic" className="cursor-pointer">
                    Make Public
                  </Label>
                  <p className="text-xs text-muted-foreground">
                    Allow others to see and use this indicator
                  </p>
                </div>
                <Switch
                  id="isPublic"
                  checked={isPublic}
                  onCheckedChange={setIsPublic}
                />
              </div>
            </TabsContent>

            <TabsContent value="advanced" className="space-y-4">
              {indicator.type === "Custom" ? (
                <FormulaEditor
                  value={formula}
                  onChange={setFormula}
                  error={error || undefined}
                />
              ) : selectedBuiltIn ? (
                <div className="space-y-4">
                  <p className="text-sm text-muted-foreground">
                    Configure parameters for {selectedBuiltIn.displayName}
                  </p>
                  {selectedBuiltIn.parameters.map((param) => (
                    <IndicatorParameterInput
                      key={param.name}
                      parameter={param}
                      value={parameters[param.name] ?? param.defaultValue}
                      onChange={(value) =>
                        setParameters((prev) => ({
                          ...prev,
                          [param.name]: value,
                        }))
                      }
                    />
                  ))}
                </div>
              ) : null}
            </TabsContent>
          </Tabs>

          {/* Actions */}
          <div className="flex gap-2 justify-end pt-4">
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              disabled={isSubmitting}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Save Changes
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
