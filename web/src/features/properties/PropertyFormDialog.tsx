import { toast } from 'sonner'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '../../components/ui/dialog'
import { PropertyForm } from './PropertyForm'
import { useCreateProperty, useProperty, useUpdateProperty } from '../../api/properties'
import type { Property } from '../../api/types'

type Props = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  customerId: string;
  propertyId?: string;
  onSaved?: (property: Property) => void;
};

export function PropertyFormDialog({ open, onOpenChange, customerId, propertyId, onSaved }: Props) {
  const isEdit = Boolean(propertyId);
  const existing = useProperty(propertyId);
  const create = useCreateProperty(customerId);
  const update = useUpdateProperty(customerId, propertyId ?? "");

  const ready = !isEdit || existing.isSuccess;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{isEdit ? "Edit property" : "Add property"}</DialogTitle>
          <DialogDescription>
            Job sites are recorded per property so we can keep the customer's history intact.
          </DialogDescription>
        </DialogHeader>

        {ready && (
          <PropertyForm
            submitLabel={isEdit ? "Save changes" : "Add property"}
            submitting={create.isPending || update.isPending}
            onCancel={() => onOpenChange(false)}
            defaultValues={
              existing.data
                ? {
                    streetAddress: existing.data.streetAddress,
                    city: existing.data.city,
                    state: existing.data.state,
                    postalCode: existing.data.postalCode,
                    accessNotes: existing.data.accessNotes ?? "",
                  }
                : undefined
            }
            onSubmit={async (values) => {
              try {
                const saved = isEdit ? await update.mutateAsync(values) : await create.mutateAsync(values);
                toast.success(isEdit ? "Property updated" : "Property added");
                onSaved?.(saved);
                onOpenChange(false);
              } catch (err) {
                toast.error(err instanceof Error ? err.message : "Something went wrong");
              }
            }}
          />
        )}
      </DialogContent>
    </Dialog>
  );
}