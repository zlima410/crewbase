import { useNavigate, useParams } from 'react-router-dom'
import { toast } from 'sonner'
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/card'
import { CustomerForm } from './CustomerForm'
import { useCreateCustomer, useCustomer, useUpdateCustomer } from '../../api/customers'

export function CustomerFormPage() {
  const { id } = useParams<{ id: string }>();
  const isEdit = Boolean(id);
  const navigate = useNavigate();

  const existing = useCustomer(id);
  const create = useCreateCustomer();
  const update = useUpdateCustomer(id ?? "");

  if (isEdit && existing.isLoading) return null;
  if (isEdit && existing.isError) {
    return <p className="text-destructive">Failed to load customer.</p>;
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{isEdit ? "Edit Customer" : "Add Customer"}</CardTitle>
      </CardHeader>
      <CardContent>
        <CustomerForm
          submitLabel={isEdit ? "Save changes" : "Create customer"}
          submitting={create.isPending || update.isPending}
          onCancel={() => navigate(isEdit ? `/customers/${id}` : "/customers")}
          defaultValues={
            existing.data
              ? {
                  firstName: existing.data.firstName,
                  lastName: existing.data.lastName,
                  phone: existing.data.phone,
                  email: existing.data.email ?? "",
                  notes: existing.data.notes ?? "",
                }
              : undefined
          }
          onSubmit={async (values) => {
            try {
              if (isEdit) {
                await update.mutateAsync(values);
                toast.success("Customer updated");
                navigate(`/customers/${id}`);
              } else {
                const created = await create.mutateAsync(values);
                toast.success("Customer created");
                navigate(`/customers/${created.id}`);
              }
            } catch (err) {
              toast.error(err instanceof Error ? err.message : "Something went wrong");
            }
          }}
        />
      </CardContent>
    </Card>
  );
}
