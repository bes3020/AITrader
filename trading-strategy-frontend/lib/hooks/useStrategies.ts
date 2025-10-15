"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import { Strategy } from "@/lib/types";

/**
 * Hook to fetch all strategies
 */
export function useStrategies(symbol?: string) {
  return useQuery({
    queryKey: ["strategies", symbol],
    queryFn: () => apiClient.listStrategies(symbol),
  });
}

/**
 * Hook to fetch strategy detail
 */
export function useStrategyDetail(id: number) {
  return useQuery({
    queryKey: ["strategy", id],
    queryFn: () => apiClient.getStrategyDetail(id),
    enabled: !!id,
  });
}

/**
 * Hook to fetch strategy versions
 */
export function useStrategyVersions(id: number) {
  return useQuery({
    queryKey: ["strategy", id, "versions"],
    queryFn: () => apiClient.getVersions(id),
    enabled: !!id,
  });
}

/**
 * Hook to search strategies
 */
export function useSearchStrategies(params: any) {
  return useQuery({
    queryKey: ["strategies", "search", params],
    queryFn: () => apiClient.searchStrategies(params),
    enabled: !!params,
  });
}

/**
 * Hook to compare strategies
 */
export function useCompareStrategies(strategyIds: number[]) {
  return useQuery({
    queryKey: ["strategies", "compare", strategyIds],
    queryFn: () => apiClient.compareStrategies(strategyIds),
    enabled: strategyIds.length > 0,
  });
}

/**
 * Hook to update a strategy
 */
export function useUpdateStrategy() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: any }) =>
      apiClient.updateStrategy(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ["strategy", variables.id] });
      queryClient.invalidateQueries({ queryKey: ["strategies"] });
    },
  });
}

/**
 * Hook to delete a strategy
 */
export function useDeleteStrategy() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: number) => apiClient.deleteStrategy(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["strategies"] });
    },
  });
}

/**
 * Hook to duplicate a strategy
 */
export function useDuplicateStrategy() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, newName }: { id: number; newName: string }) =>
      apiClient.duplicateStrategy(id, newName),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["strategies"] });
    },
  });
}

/**
 * Hook to create a version
 */
export function useCreateVersion() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: any }) =>
      apiClient.createVersion(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ["strategy", variables.id, "versions"] });
      queryClient.invalidateQueries({ queryKey: ["strategies"] });
    },
  });
}

/**
 * Hook to toggle favorite
 */
export function useToggleFavorite() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: number) => apiClient.toggleFavorite(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ["strategy", id] });
      queryClient.invalidateQueries({ queryKey: ["strategies"] });
    },
  });
}

/**
 * Hook to archive/unarchive a strategy
 */
export function useArchiveStrategy() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, archive }: { id: number; archive: boolean }) =>
      apiClient.archiveStrategy(id, archive),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ["strategy", variables.id] });
      queryClient.invalidateQueries({ queryKey: ["strategies"] });
    },
  });
}

/**
 * Hook to export a strategy
 */
export function useExportStrategy() {
  return useMutation({
    mutationFn: (id: number) => apiClient.exportStrategy(id),
  });
}

/**
 * Hook to import a strategy
 */
export function useImportStrategy() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: any) => apiClient.importStrategy(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["strategies"] });
    },
  });
}
