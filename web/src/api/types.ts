export type Customer = {
  id: string;
  firstName: string;
  lastName: string;
  email: string | null;
  phone: string;
  notes: string | null;
  createdAt: string;
  updatedAt: string;
};

export type CustomerListResponse = {
  items: Customer[];
  page: number;
  pageSize: number;
  total: number;
};

export type CreateCustomerRequest = {
  firstName: string;
  lastName: string;
  email?: string | null;
  phone: string;
  notes?: string | null;
};

export type UpdateCustomerRequest = CreateCustomerRequest;