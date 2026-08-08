import { z } from "zod";

export const propertySchema = z.object({
  streetAddress: z.string().trim().min(1, "Street address is required").max(300),
  city: z.string().trim().min(1, "City is required").max(100),
  state: z.string().trim().min(1, "State is required").max(50),
  postalCode: z.string().trim().min(1, "Postal code is required").max(20),
  accessNotes: z
    .union([z.string().trim().max(2000), z.literal("")])
    .optional()
    .transform((v) => (v ? v : null)),
});

export type PropertyFormValues = z.input<typeof propertySchema>;
export type PropertyFormOutput = z.output<typeof propertySchema>;