import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { api } from "../lib/apiClient";
import type { JobListResponse, JobStatus } from "./types";

export const jobKeys = {
  all: ["jobs"] as const,
  list: (page: number, size: number, status?: JobStatus, search?: string) =>
    [...jobKeys.all, "list", { page, size, status, search }] as const,
};

export function useJobs(
  page: number,
  pageSize: number,
  status?: JobStatus,
  search?: string,
  enabled = true,
) {
  return useQuery({
    queryKey: jobKeys.list(page, pageSize, status, search),
    queryFn: () => {
      const qs = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
      if (status) qs.set("status", status);
      if (search) qs.set("search", search);
      return api<JobListResponse>(`/api/v1/jobs?${qs}`);
    },
    placeholderData: keepPreviousData,
    enabled,
  });
}
