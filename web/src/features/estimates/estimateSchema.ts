import { z } from "zod";

export const roomSchema = z.object({
  id: z.string().uuid().optional().nullable(),
  name: z.string().trim().min(1, "Name required").max(100),
  lengthFeet: z.coerce.number().gt(0, "> 0").lte(1000),
  widthFeet: z.coerce.number().gt(0, "> 0").lte(1000),
  wastePercentage: z.coerce.number().min(0).max(100),
  flooringType: z.enum(["SolidHardwood", "EngineeredHardwood", "ExistingHardwood", "Other"]),
  workType: z.enum(["NewInstallation", "Refinishing", "Repair", "ScreenAndRecoat", "Removal"]),
  laborRatePerSqFt: z.coerce.number().min(0).max(10_000),
  materialRatePerSqFt: z.coerce.number().min(0).max(10_000),
});

export const estimateSchema = z.object({
  customerId: z.string().uuid("Select a customer"),
  propertyId: z.string().uuid("Select a property"),
  expirationDate: z.string().datetime().nullable().optional(),
  taxRate: z.coerce.number().min(0).max(100),
  notes: z
    .string()
    .max(4000)
    .optional()
    .nullable()
    .transform((v) => (v ? v : null)),
  rooms: z.array(roomSchema).min(1, "Add at least one room"),
});

export type EstimateFormValues = z.input<typeof estimateSchema>;
export type EstimateFormOutput = z.output<typeof estimateSchema>;