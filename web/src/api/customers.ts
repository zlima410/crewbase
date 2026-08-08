import { useMutation, useQuery, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import { api } from "../lib/apiClient";
import type { CreateCustomerRequest, Customer, CustomerListResponse, UpdateCustomerRequest } from "./types";

const keys = {
  all: ["customers"] as const,
  list: (search: string, page: number, pageSize: number) =>
    [...keys.all, "list", { search, page, pageSize }] as const,
  detail: (id: string) => [...keys.all, "detail", id] as const
};

export function useCustomers(search: string, page: number, pageSize: number) {
  return useQuery({
    queryKey: keys.list(search, page, pageSize),
    queryFn: () => {
      const qs = new URLSearchParams();
      if (search) qs.set("search", search);
      qs.set("page", String(page));
      qs.set("pageSize", String(pageSize));
      return api<CustomerListResponse>(`/api/v1/customers?${qs}`);
    },
    placeholderData: keepPreviousData,
  });
}

export function useCustomer(id: string | undefined) {
  return useQuery({
    queryKey: id ? keys.detail(id) : ["customers", "detail", "disabled"],
    queryFn: () => api<Customer>(`/api/v1/customers/${id}`),
    enabled: !!id
  });
}

export function useCreateCustomer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateCustomerRequest) =>
      api<Customer>("/api/v1/customers", {
        method: "POST",
        body: JSON.stringify(body)
      }),
    onSuccess: () => qc.invalidateQueries({ queryKey: keys.all })
  });
}

export function useUpdateCustomer(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: UpdateCustomerRequest) =>
      api<Customer>(`/api/v1/customers/${id}`, {
        method: "PUT",
        body: JSON.stringify(body)
      }),
    onSuccess: (updated) => {
      qc.setQueryData(keys.detail(id), updated);
      qc.invalidateQueries({ queryKey: keys.all });
    },
  });
}