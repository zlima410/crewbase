import { z } from "zod";

const envSchema = z.object({
  VITE_API_BASE_URL: z
    .string()
    .url("VITE_API_BASE_URL must be an absolute URL, e.g. https://localhost:7069")
    .transform((value) => value.replace(/\/+$/, "")),
  VITE_SUPABASE_URL: z.string().url("VITE_SUPABASE_URL must be an absolute URL"),
  VITE_SUPABASE_ANON_KEY: z.string().min(1, "VITE_SUPABASE_ANON_KEY is required"),
});

const parsed = envSchema.safeParse(import.meta.env);

if (!parsed.success) {
  const details = parsed.error.issues
    .map((issue) => `  - ${issue.path.join(".")}: ${issue.message}`)
    .join("\n");

  throw new Error(
    `Invalid frontend configuration.\n${details}\n\n` +
      "Copy .env.example to .env.local and fill in the values.",
  );
}

export const env = {
  apiBaseUrl: parsed.data.VITE_API_BASE_URL,
  supabaseUrl: parsed.data.VITE_SUPABASE_URL,
  supabaseAnonKey: parsed.data.VITE_SUPABASE_ANON_KEY,
} as const;
