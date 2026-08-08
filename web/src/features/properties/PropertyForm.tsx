import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Button } from '../../components/ui/button'
import { Input } from '../../components/ui/input'
import { Textarea } from '../../components/ui/textarea'
import { Label } from '../../components/ui/label'
import { propertySchema, type PropertyFormOutput, type PropertyFormValues } from './propertySchema'

type Props = {
  defaultValues?: PropertyFormValues;
  submitting?: boolean;
  submitLabel: string;
  onSubmit: (values: PropertyFormOutput) => void | Promise<void>;
  onCancel?: () => void;
};

export function PropertyForm({ defaultValues, submitting, submitLabel, onSubmit, onCancel }: Props) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<PropertyFormValues, unknown, PropertyFormOutput>({
    resolver: zodResolver(propertySchema),
    defaultValues: defaultValues ?? {
      streetAddress: "",
      city: "",
      state: "",
      postalCode: "",
      accessNotes: "",
    },
  });

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <Field label="Street address" error={errors.streetAddress?.message}>
        <Input {...register("streetAddress")} autoFocus />
      </Field>
      <div className="grid grid-cols-[1fr_6rem_8rem] gap-4">
        <Field label="City" error={errors.city?.message}>
          <Input {...register("city")} />
        </Field>
        <Field label="State" error={errors.state?.message}>
          <Input {...register("state")} maxLength={50} />
        </Field>
        <Field label="Postal code" error={errors.postalCode?.message}>
          <Input {...register("postalCode")} inputMode="numeric" />
        </Field>
      </div>
      <Field label="Access notes" error={errors.accessNotes?.message}>
        <Textarea
          {...register("accessNotes")}
          rows={3}
          placeholder="Gate code, dog on premises, garage on right, etc."
        />
      </Field>
      <div className="flex gap-2 justify-end">
        {onCancel && (
          <Button type="button" variant="ghost" onClick={onCancel}>
            Cancel
          </Button>
        )}
        <Button type="submit" disabled={submitting}>
          {submitLabel}
        </Button>
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