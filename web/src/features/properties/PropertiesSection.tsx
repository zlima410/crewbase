import { useState } from 'react'
import { Button } from '../../components/ui/button'
import { Skeleton } from '../../components/ui/skeleton'
import { useProperties } from '../../api/properties'
import type { Property } from '../../api/types'
import { PropertyFormDialog } from './PropertyFormDialog'

export function PropertiesSection({ customerId }: { customerId: string }) {
  const { data, isLoading } = useProperties(customerId);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<Property | null>(null);

  const openAdd = () => {
    setEditing(null);
    setDialogOpen(true);
  };
  const openEdit = (p: Property) => {
    setEditing(p);
    setDialogOpen(true);
  };

  return (
    <section className="border-t pt-4">
      <div className="flex items-center justify-between mb-3">
        <h3 className="font-medium">Properties</h3>
        <Button size="sm" onClick={openAdd}>
          Add property
        </Button>
      </div>

      {isLoading ? (
        <Skeleton className="h-16 w-full" />
      ) : !data || data.length === 0 ? (
        <p className="text-muted-foreground text-sm">No properties yet. Add the first job site for this customer.</p>
      ) : (
        <ul className="divide-y rounded-md border">
          {data.map((p) => (
            <li key={p.id} className="flex items-start justify-between gap-4 p-3">
              <div className="text-sm">
                <div className="font-medium">{p.streetAddress}</div>
                <div className="text-muted-foreground">
                  {p.city}, {p.state} {p.postalCode}
                </div>
                {p.accessNotes && (
                  <div className="text-muted-foreground mt-1">
                    <span className="font-medium">Access:</span> {p.accessNotes}
                  </div>
                )}
              </div>
              <Button variant="ghost" size="sm" onClick={() => openEdit(p)}>
                Edit
              </Button>
            </li>
          ))}
        </ul>
      )}

      <PropertyFormDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        customerId={customerId}
        propertyId={editing?.id}
      />
    </section>
  );
}