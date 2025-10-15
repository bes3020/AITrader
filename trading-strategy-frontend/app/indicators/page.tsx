"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Plus, Search, Loader2, AlertCircle } from "lucide-react";
import { BuiltInIndicatorCard } from "@/components/indicators/BuiltInIndicatorCard";
import { CustomIndicatorCard } from "@/components/indicators/CustomIndicatorCard";
import { CreateIndicatorDialog } from "@/components/indicators/CreateIndicatorDialog";
import { EditIndicatorDialog } from "@/components/indicators/EditIndicatorDialog";
import {
  useBuiltInIndicators,
  useMyIndicators,
  usePublicIndicators,
  useCreateIndicator,
  useUpdateIndicator,
  useDeleteIndicator,
} from "@/lib/hooks/useIndicators";
import type { BuiltInIndicator, CustomIndicator, CreateIndicatorRequest, UpdateIndicatorRequest } from "@/lib/types/indicator";

export default function IndicatorsPage() {
  const [searchQuery, setSearchQuery] = useState("");
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [cloneFrom, setCloneFrom] = useState<BuiltInIndicator | null>(null);
  const [editingIndicator, setEditingIndicator] = useState<CustomIndicator | null>(null);

  // Queries
  const { data: builtInIndicators = [], isLoading: isLoadingBuiltIn, error: builtInError } = useBuiltInIndicators();
  const { data: myIndicators = [], isLoading: isLoadingMy, error: myError } = useMyIndicators();
  const { data: publicIndicators = [], isLoading: isLoadingPublic, error: publicError } = usePublicIndicators();

  // Mutations
  const createMutation = useCreateIndicator();
  const updateMutation = useUpdateIndicator();
  const deleteMutation = useDeleteIndicator();

  // Filter indicators by search query
  const filteredBuiltIn = builtInIndicators.filter(
    (indicator) =>
      indicator.displayName.toLowerCase().includes(searchQuery.toLowerCase()) ||
      indicator.description.toLowerCase().includes(searchQuery.toLowerCase()) ||
      indicator.type.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const filteredMy = myIndicators.filter(
    (indicator) =>
      indicator.displayName.toLowerCase().includes(searchQuery.toLowerCase()) ||
      (indicator.description?.toLowerCase() || "").includes(searchQuery.toLowerCase()) ||
      indicator.type.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const filteredPublic = publicIndicators.filter(
    (indicator) =>
      indicator.displayName.toLowerCase().includes(searchQuery.toLowerCase()) ||
      (indicator.description?.toLowerCase() || "").includes(searchQuery.toLowerCase()) ||
      indicator.type.toLowerCase().includes(searchQuery.toLowerCase())
  );

  // Handlers
  const handleClone = (indicator: BuiltInIndicator) => {
    setCloneFrom(indicator);
    setCreateDialogOpen(true);
  };

  const handleUseBuiltIn = (indicator: BuiltInIndicator) => {
    // TODO: Navigate to strategy creation with this indicator pre-selected
    console.log("Using built-in indicator:", indicator);
  };

  const handleCreate = async (data: CreateIndicatorRequest) => {
    await createMutation.mutateAsync(data);
    setCloneFrom(null);
  };

  const handleEdit = (indicator: CustomIndicator) => {
    setEditingIndicator(indicator);
    setEditDialogOpen(true);
  };

  const handleUpdate = async (id: number, data: UpdateIndicatorRequest) => {
    await updateMutation.mutateAsync({ id, data });
  };

  const handleDelete = async (id: number) => {
    if (confirm("Are you sure you want to delete this indicator?")) {
      await deleteMutation.mutateAsync(id);
    }
  };

  const handleTogglePublic = async (id: number, isPublic: boolean) => {
    await updateMutation.mutateAsync({ id, data: { isPublic } });
  };

  const handleUseCustom = (indicator: CustomIndicator) => {
    // TODO: Navigate to strategy creation with this indicator pre-selected
    console.log("Using custom indicator:", indicator);
  };

  const isLoading = isLoadingBuiltIn || isLoadingMy || isLoadingPublic;

  return (
    <div className="container mx-auto py-8 px-4">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold mb-2">Technical Indicators</h1>
        <p className="text-muted-foreground">
          Use built-in indicators or create custom indicators to power your trading strategies
        </p>
      </div>

      {/* Search and Actions */}
      <div className="flex gap-4 mb-6">
        <div className="flex-1 relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search indicators..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="pl-9"
          />
        </div>
        <Button onClick={() => setCreateDialogOpen(true)}>
          <Plus className="mr-2 h-4 w-4" />
          Create Indicator
        </Button>
      </div>

      {/* Error Alerts */}
      {(builtInError || myError || publicError) && (
        <Alert variant="destructive" className="mb-6">
          <AlertCircle className="h-4 w-4" />
          <AlertDescription>
            {builtInError?.message || myError?.message || publicError?.message || "Failed to load indicators"}
          </AlertDescription>
        </Alert>
      )}

      {/* Loading State */}
      {isLoading ? (
        <div className="flex items-center justify-center py-12">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
        </div>
      ) : (
        <Tabs defaultValue="built-in" className="w-full">
          <TabsList className="grid w-full grid-cols-3">
            <TabsTrigger value="built-in">
              Built-In ({filteredBuiltIn.length})
            </TabsTrigger>
            <TabsTrigger value="my">
              My Indicators ({filteredMy.length})
            </TabsTrigger>
            <TabsTrigger value="public">
              Public ({filteredPublic.length})
            </TabsTrigger>
          </TabsList>

          {/* Built-In Indicators */}
          <TabsContent value="built-in" className="mt-6">
            {filteredBuiltIn.length === 0 ? (
              <div className="text-center py-12">
                <p className="text-muted-foreground">No built-in indicators found</p>
              </div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {filteredBuiltIn.map((indicator) => (
                  <BuiltInIndicatorCard
                    key={indicator.type}
                    indicator={indicator}
                    onClone={handleClone}
                    onUse={handleUseBuiltIn}
                  />
                ))}
              </div>
            )}
          </TabsContent>

          {/* My Indicators */}
          <TabsContent value="my" className="mt-6">
            {filteredMy.length === 0 ? (
              <div className="text-center py-12">
                <p className="text-muted-foreground mb-4">
                  {searchQuery
                    ? "No indicators match your search"
                    : "You haven't created any custom indicators yet"}
                </p>
                {!searchQuery && (
                  <Button onClick={() => setCreateDialogOpen(true)}>
                    <Plus className="mr-2 h-4 w-4" />
                    Create Your First Indicator
                  </Button>
                )}
              </div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {filteredMy.map((indicator) => (
                  <CustomIndicatorCard
                    key={indicator.id}
                    indicator={indicator}
                    onEdit={handleEdit}
                    onDelete={handleDelete}
                    onTogglePublic={handleTogglePublic}
                    onUse={handleUseCustom}
                  />
                ))}
              </div>
            )}
          </TabsContent>

          {/* Public Indicators */}
          <TabsContent value="public" className="mt-6">
            {filteredPublic.length === 0 ? (
              <div className="text-center py-12">
                <p className="text-muted-foreground">
                  {searchQuery
                    ? "No public indicators match your search"
                    : "No public indicators available yet"}
                </p>
              </div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {filteredPublic.map((indicator) => (
                  <CustomIndicatorCard
                    key={indicator.id}
                    indicator={indicator}
                    onEdit={handleEdit}
                    onDelete={handleDelete}
                    onTogglePublic={handleTogglePublic}
                    onUse={handleUseCustom}
                  />
                ))}
              </div>
            )}
          </TabsContent>
        </Tabs>
      )}

      {/* Dialogs */}
      <CreateIndicatorDialog
        open={createDialogOpen}
        onOpenChange={(open) => {
          setCreateDialogOpen(open);
          if (!open) setCloneFrom(null);
        }}
        onCreate={handleCreate}
        cloneFrom={cloneFrom}
        builtInIndicators={builtInIndicators}
      />

      <EditIndicatorDialog
        open={editDialogOpen}
        onOpenChange={(open) => {
          setEditDialogOpen(open);
          if (!open) setEditingIndicator(null);
        }}
        indicator={editingIndicator}
        onUpdate={handleUpdate}
        builtInIndicators={builtInIndicators}
      />
    </div>
  );
}
