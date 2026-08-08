import { z } from 'zod'

export const customerSchema = z.object({
  firstName: z.string().trim().min(1, "First name is required").max(100),
  lastName: z.string().trim().min(1, "Last name is required").max(100),
  phone: z.string().trim().min(1, "Phone is required").max(50),
  email: z
    .union([z.string().trim().email("Invalid email").max(320), z.literal("")])
    .optional()
    .transform((v) => (v ? v : null)),
  notes: z
    .union([z.string().trim().max(2000), z.literal("")])
    .optional()
    .transform((v) => (v ? v : null))
});

export type CustomerFormValues = z.input<typeof customerSchema>;
export type CustomerFormOutput = z.output<typeof customerSchema>;