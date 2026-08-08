import { Link, useParams } from 'react-router-dom'
import { Button } from '../../components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/card'
import { Skeleton } from '../../components/ui/skeleton'
import { useCustomer } from '../../api/customers'

export function CustomerDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data, isLoading, isError } = useCustomer(id);

  if (isLoading) return <Skeleton className="h-64 w-full" />;
  if (isError || !data) return <p>Customer not found.</p>;

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle>
          {data.firstName} {data.lastName}
        </CardTitle>
        <div className="flex gap-2">
          <Button variant="outline" asChild>
            <Link to="/customers">Back</Link>
          </Button>
          <Button asChild>
            <Link to={`/customers/${data.id}/edit`}>Edit</Link>
          </Button>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        <Info label="Phone" value={data.phone} />
        <Info label="Email" value={data.email ?? "—"} />
        <Info label="Notes" value={data.notes ?? "—"} />
        <Info label="Created" value={new Date(data.createdAt).toLocaleString()} />
        <Info label="Updated" value={new Date(data.updatedAt).toLocaleString()} />

        <section className="border-t pt-4">
          <h3 className="font-medium mb-2">Properties</h3>
          <p className="text-muted-foreground text-sm">Coming in Phase 4.</p>
        </section>
      </CardContent>
    </Card>
  );
}

function Info({ label, value }: { label: string; value: string }) {
  return (
    <div className="grid grid-cols-[8rem_1fr] gap-4">
      <span className="text-muted-foreground text-sm">{label}</span>
      <span>{value}</span>
    </div>
  );
}