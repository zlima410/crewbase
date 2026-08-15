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

export type EstimateStatus = "Draft" | "Sent" | "Accepted" | "Rejected" | "Expired";
export type FlooringType = "SolidHardwood" | "EngineeredHardwood" | "ExistingHardwood" | "Other";
export type WorkType = "NewInstallation" | "Refinishing" | "Repair" | "ScreenAndRecoat" | "Removal";
export type InstallationMethod = "NailDown" | "GlueDown" | "Floating" | "Existing" | "Unknown";
export type FinishType = "WaterBased" | "OilBased" | "Unfinished" | "PreFinished" | "Other";

export type EstimateRoom = {
  id: string;
  name: string;
  lengthFeet: number;
  widthFeet: number;
  wastePercentage: number;
  squareFeet: number;
  billableSquareFeet: number;
  flooringType: FlooringType;
  workType: WorkType;
  installationMethod: InstallationMethod | null;
  finishType: FinishType | null;
  notes: string | null;
  laborRatePerSqFt: number;
  materialRatePerSqFt: number;
  laborCost: number;
  materialCost: number;
  roomTotal: number;
  position: number;
};

export type Estimate = {
  id: string;
  estimateNumber: string;
  status: EstimateStatus;
  customerId: string;
  customerName: string;
  propertyId: string;
  propertyAddress: string;
  createdDate: string;
  expirationDate: string | null;
  updatedAt: string;
  laborSubtotal: number;
  materialSubtotal: number;
  taxRate: number;
  tax: number;
  subtotal: number;
  total: number;
  notes: string | null;
  rooms: EstimateRoom[];
};

export type EstimateRoomInput = {
  id?: string | null;
  name: string;
  lengthFeet: number;
  widthFeet: number;
  wastePercentage: number;
  flooringType: FlooringType;
  workType: WorkType;
  installationMethod?: InstallationMethod | null;
  finishType?: FinishType | null;
  notes?: string | null;
  laborRatePerSqFt: number;
  materialRatePerSqFt: number;
};

export type CreateEstimateRequest = {
  customerId: string;
  propertyId: string;
  expirationDate?: string | null;
  taxRate: number;
  notes?: string | null;
  rooms: EstimateRoomInput[];
};

export type UpdateEstimateRequest = CreateEstimateRequest;

export type EstimateListItem = {
  id: string;
  estimateNumber: string;
  status: EstimateStatus;
  customerId: string;
  customerName: string;
  propertyAddress: string;
  total: number;
  createdDate: string;
};

export type EstimateListResponse = {
  items: EstimateListItem[];
  page: number;
  pageSize: number;
  total: number;
};

export type JobStatus = "Scheduled" | "InProgress" | "Waiting" | "Completed" | "Cancelled";

export type JobRoom = {
  id: string;
  name: string;
  squareFeet: number;
  billableSquareFeet: number;
  flooringType: FlooringType;
  workType: WorkType;
  installationMethod: InstallationMethod | null;
  finishType: FinishType | null;
  notes: string | null;
  position: number;
};

export type Job = {
  id: string;
  jobNumber: string;
  status: JobStatus;
  estimateId: string;
  customerId: string;
  customerName: string;
  propertyId: string;
  propertyAddress: string;
  description: string | null;
  internalNotes: string | null;
  customerNotes: string | null;
  scheduledStart: string | null;
  scheduledEnd: string | null;
  actualStart: string | null;
  actualEnd: string | null;
  createdAt: string;
  rooms: JobRoom[];
};