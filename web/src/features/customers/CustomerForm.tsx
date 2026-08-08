import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Button } from '../../components/ui/button'
import { Input } from '../../components/ui/input'
import { Textarea } from '../../components/ui/textarea'
import { Label } from '../../components/ui/label'
import { customerSchema, type CustomerFormOutput, type CustomerFormValues } from './customerSchema'

type Props = {
  defaultValues?: CustomerFormValues;
  submitting?: boolean;
  submitLabel: string;
  onSubmit: (values: CustomerFormOutput) => void | Promise<void>;
  onCancel?: () => void;
};

export function CustomerForm({ defaultValues, submitting, submitLabel, onSubmit, onCancel }: Props) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<CustomerFormValues, unknown, CustomerFormOutput>({
    resolver: zodResolver(customerSchema),
    defaultValues: defaultValues ?? {
      firstName: "",
      lastName: "",
      phone: "",
      email: "",
      notes: "",
    },
  });

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 max-w-xl">
      <div className="grid grid-cols-2 gap-4">
        <Field label="First name" error={errors.firstName?.message}>
          <Input {...register("firstName")} autoFocus />
        </Field>
        <Field label="Last name" error={errors.lastName?.message}>
          <Input {...register("lastName")} />
        </Field>
      </div>
      <Field label="Phone" error={errors.phone?.message}>
        <Input {...register("phone")} inputMode="tel" />
      </Field>
      <Field label="Email" error={errors.email?.message}>
        <Input {...register("email")} type="email" />
      </Field>
      <Field label="Notes" error={errors.notes?.message}>
        <Textarea {...register("notes")} rows={4} />
      </Field>
      <div className="flex gap-2">
        <Button type="submit" disabled={submitting}>
          {submitLabel}
        </Button>
        {onCancel && (
          <Button type="button" variant="ghost" onClick={onCancel}>
            Cancel
          </Button>
        )}
      </div>
    </form>
  );
}

function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1.5">
      <Label>{label}</Label>
      {children}
      {error && <p className="text-sm text-destructive">{error}</p>}
    </div>
  );
}
