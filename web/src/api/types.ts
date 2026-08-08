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

export type Property = {
  id: string;
  customerId: string;
  streetAddress: string;
  city: string;
  state: string;
  postalCode: string;
  accessNotes: string | null;
  createdAt: string;
  updatedAt: string;
};

export type CreatePropertyRequest = {
  streetAddress: string;
  city: string;
  state: string;
  postalCode: string;
  accessNotes?: string | null;
};

export type UpdatePropertyRequest = CreatePropertyRequest;