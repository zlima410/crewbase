import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../lib/apiClient'
import type { CreatePropertyRequest, Property, UpdatePropertyRequest } from './types'

const keys = {
  byCustomer: (customerId: string) => ["customers", customerId, "properties"] as const,
  detail: (id: string) => ["properties", "detail", id] as const,
};

export function useProperties(customerId: string | undefined) {
  return useQuery({
    queryKey: customerId ? keys.byCustomer(customerId) : ["properties", "disabled"],
    queryFn: () => api<Property[]>(`/api/v1/customers/${customerId}/properties`),
    enabled: !!customerId,
  });
}

export function useProperty(id: string | undefined) {
  return useQuery({
    queryKey: id ? keys.detail(id) : ["properties", "detail", "disabled"],
    queryFn: () => api<Property>(`/api/v1/properties/${id}`),
    enabled: !!id,
  });
}

export function useCreateProperty(customerId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreatePropertyRequest) =>
      api<Property>(`/api/v1/customers/${customerId}/properties`, {
        method: "POST",
        body: JSON.stringify(body),
      }),
    onSuccess: () => qc.invalidateQueries({ queryKey: keys.byCustomer(customerId) }),
  });
}

export function useUpdateProperty(customerId: string, propertyId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: UpdatePropertyRequest) =>
      api<Property>(`/api/v1/properties/${propertyId}`, {
        method: "PUT",
        body: JSON.stringify(body),
      }),
    onSuccess: (updated) => {
      qc.setQueryData(keys.detail(propertyId), updated);
      qc.invalidateQueries({ queryKey: keys.byCustomer(customerId) });
    },
  });
}