import { useMutation, useQuery, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { api } from '../lib/apiClient'
import { jobKeys } from './jobs'
import type {
  CreateEstimateRequest,
  Estimate,
  EstimateListResponse,
  EstimateStatus,
  Job,
  UpdateEstimateRequest,
} from './types'

const keys = {
  all: ["estimates"] as const,
  list: (page: number, size: number, status?: EstimateStatus) => [...keys.all, "list", { page, size, status }] as const,
  detail: (id: string) => [...keys.all, "detail", id] as const,
};

export function useEstimates(page: number, pageSize: number, status?: EstimateStatus) {
  return useQuery({
    queryKey: keys.list(page, pageSize, status),
    queryFn: () => {
      const qs = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
      if (status) qs.set("status", status);
      return api<EstimateListResponse>(`/api/v1/estimates?${qs}`);
    },
    placeholderData: keepPreviousData,
  });
}

export function useEstimate(id: string | undefined) {
  return useQuery({
    queryKey: id ? keys.detail(id) : ["estimates", "detail", "disabled"],
    queryFn: () => api<Estimate>(`/api/v1/estimates/${id}`),
    enabled: !!id,
  });
}

export function useCreateEstimate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateEstimateRequest) =>
      api<Estimate>("/api/v1/estimates", { method: "POST", body: JSON.stringify(body) }),
    onSuccess: () => qc.invalidateQueries({ queryKey: keys.all }),
  });
}

export function useUpdateEstimate(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: UpdateEstimateRequest) =>
      api<Estimate>(`/api/v1/estimates/${id}`, { method: "PUT", body: JSON.stringify(body) }),
    onSuccess: (updated) => {
      qc.setQueryData(keys.detail(id), updated);
      qc.invalidateQueries({ queryKey: keys.all });
    },
  });
}

export function useSendEstimate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api<Estimate>(`/api/v1/estimates/${id}/send`, { method: "POST" }),
    onSuccess: (updated) => {
      qc.setQueryData(keys.detail(updated.id), updated);
      qc.invalidateQueries({ queryKey: keys.all });
    },
  });
}

export function useAcceptEstimate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api<Job>(`/api/v1/estimates/${id}/accept`, { method: "POST" }),
    onSuccess: (job, id) => {
      qc.invalidateQueries({ queryKey: keys.all });
      qc.invalidateQueries({ queryKey: keys.detail(id) });
      qc.setQueryData(jobKeys.detail(job.id), job);
      qc.invalidateQueries({ queryKey: jobKeys.all });
    },
  });
}