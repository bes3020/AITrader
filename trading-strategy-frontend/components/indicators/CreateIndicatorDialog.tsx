"use client";

import { useState } from "react";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { Switch } from "@/components/ui/switch";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Loader2 } from "lucide-react";
import { FormulaEditor } from "./FormulaEditor";
import { IndicatorParameterInput } from "./IndicatorParameterInput";
import type { BuiltInIndicator, IndicatorParameter, CreateIndicatorRequest } from "@/lib/types/indicator";

interface CreateIndicatorDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCreate: (data: CreateIndicatorRequest) => Promise<void>;
  cloneFrom?: BuiltInIndicator | null;
  builtInIndicators: BuiltInIndicator[];
}

export function CreateIndicatorDialog({
  open,
  onOpenChange,
  onCreate,
  cloneFrom,
  builtInIndicators,
}: CreateIndicatorDialogProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Form state
  const [name, setName] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [type, setType] = useState<string>(cloneFrom?.type || "Custom");
  const [description, setDescription] = useState("");
  const [isPublic, setIsPublic] = useState(false);
  const [formula, setFormula] = useState("");

  // Parameters state
  const [parameters, setParameters] = useState<Record<string, any>>(() => {
    if (cloneFrom) {
      const params: Record<string, any> = {};
      cloneFrom.parameters.forEach(p => {
        params[p.name] = p.defaultValue;
      });
      return params;
    }
    return {};
  });

  // Reset form when dialog opens/closes or cloneFrom changes
  useState(() => {
    if (cloneFrom) {
      setName(cloneFrom.type.toLowerCase() + "_custom");
      setDisplayName(cloneFrom.displayName + " (Custom)");
      setType(cloneFrom.type);
      setDescription(cloneFrom.description);
      const params: Record<string, any> = {};
      cloneFrom.parameters.forEach(p => {
        params[p.name] = p.defaultValue;
      });
      setParameters(params);
    }
  });

  const selectedBuiltIn = type !== "Custom"
    ? builtInIndicators.find(i => i.type === type)
    : null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      const data: CreateIndicatorRequest = {
        name: name.trim(),
        displayName: displayName.trim(),
        type,
        parameters: JSON.stringify(parameters),
        description: description.trim() || undefined,
        isPublic,
        formula: type === "Custom" ? formula.trim() : undefined,
      };

      await onCreate(data);

      // Reset form
      setName("");
      setDisplayName("");
      setType("Custom");
      setDescription("");
      setIsPublic(false);
      setFormula("");
      setParameters({});

      onOpenChange(false);
    } catch (err: any) {
      setError(err.message || "Failed to create indicator");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>
            {cloneFrom ? `Clone ${cloneFrom.displayName}` : "Create Custom Indicator"}
          </DialogTitle>
          <DialogDescription>
            {type === "Custom"
              ? "Create a custom indicator with your own formula"
              : "Customize parameters for a built-in indicator"}
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
                {type === "Custom" ? "Formula" : "Parameters"}
              </TabsTrigger>
            </TabsList>

            <TabsContent value="basic" className="space-y-4">
              {/* Type Selection */}
              <div className="space-y-2">
                <Label htmlFor="type">
                  Indicator Type
                  <span className="text-destructive ml-1">*</span>
                </Label>
                <Select value={type} onValueChange={setType}>
                  <SelectTrigger id="type">
                    <SelectValue placeholder="Select type" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Custom">Custom Formula</SelectItem>
                    {builtInIndicators.map(indicator => (
                      <SelectItem key={indicator.type} value={indicator.type}>
                        {indicator.displayName}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              {/* Name */}
              <div className="space-y-2">
                <Label htmlFor="name">
                  Unique Name
                  <span className="text-destructive ml-1">*</span>
                </Label>
                <Input
                  id="name"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="e.g., my_ema_20"
                  pattern="[a-z0-9_]+"
                  title="Lowercase letters, numbers, and underscores only"
                  required
                />
                <p className="text-xs text-muted-foreground">
                  Used as identifier. Lowercase, numbers, and underscores only.
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
              {type === "Custom" ? (
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
              Create Indicator
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
