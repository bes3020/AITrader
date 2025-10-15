"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type { BuiltInIndicator, CustomIndicator, CreateIndicatorRequest, UpdateIndicatorRequest } from "@/lib/types/indicator";

/**
 * Hook to fetch built-in indicators
 */
export function useBuiltInIndicators() {
  return useQuery<BuiltInIndicator[]>({
    queryKey: ["indicators", "built-in"],
    queryFn: () => apiClient.getBuiltInIndicators(),
    staleTime: Infinity, // Built-in indicators don't change
  });
}

/**
 * Hook to fetch user's custom indicators
 */
export function useMyIndicators() {
  return useQuery<CustomIndicator[]>({
    queryKey: ["indicators", "my"],
    queryFn: () => apiClient.getMyIndicators(),
  });
}

/**
 * Hook to fetch public indicators
 */
export function usePublicIndicators() {
  return useQuery<CustomIndicator[]>({
    queryKey: ["indicators", "public"],
    queryFn: () => apiClient.getPublicIndicators(),
  });
}

/**
 * Hook to create a custom indicator
 */
export function useCreateIndicator() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateIndicatorRequest) => apiClient.createIndicator(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["indicators", "my"] });
    },
  });
}

/**
 * Hook to update a custom indicator
 */
export function useUpdateIndicator() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: UpdateIndicatorRequest }) =>
      apiClient.updateIndicator(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["indicators", "my"] });
    },
  });
}

/**
 * Hook to delete a custom indicator
 */
export function useDeleteIndicator() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: number) => apiClient.deleteIndicator(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["indicators", "my"] });
    },
  });
}

/**
 * Hook to calculate indicator values
 */
export function useCalculateIndicator() {
  return useMutation({
    mutationFn: ({
      id,
      symbol,
      startDate,
      endDate,
    }: {
      id: number;
      symbol: string;
      startDate: string;
      endDate: string;
    }) => apiClient.calculateIndicator(id, symbol, startDate, endDate),
  });
}
