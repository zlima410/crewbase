import { useEffect, useRef } from "react";
import { FormProvider, useFieldArray, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useNavigate, useParams } from "react-router-dom";
import { toast } from "sonner";
import { Button } from "../../components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "../../components/ui/card";
import { Textarea } from "../../components/ui/textarea";
import { Input } from "../../components/ui/input";
import { CustomerPicker } from "./CustomerPicker";
import { PropertyPicker } from "./PropertyPicker";
import { RoomRow } from "./RoomRow";
import {
  estimateSchema,
  UNSPECIFIED,
  type EstimateFormOutput,
  type EstimateFormValues,
} from "./estimateSchema";
import { estimateTotals, fmt } from "./pricingMath";
import { useCreateEstimate, useEstimate, useUpdateEstimate } from "../../api/estimates";

const emptyRoom = (): EstimateFormValues["rooms"][number] => ({
  name: "",
  lengthFeet: 0,
  widthFeet: 0,
  wastePercentage: 10,
  flooringType: "SolidHardwood",
  workType: "NewInstallation",
  installationMethod: UNSPECIFIED,
  finishType: UNSPECIFIED,
  notes: "",
  laborRatePerSqFt: 0,
  materialRatePerSqFt: 0,
});

export function EstimateBuilderPage() {
  const { id } = useParams<{ id?: string }>();
  const isEdit = Boolean(id);
  const navigate = useNavigate();

  const existing = useEstimate(id);
  const create = useCreateEstimate();
  const update = useUpdateEstimate(id ?? "");

  const methods = useForm<EstimateFormValues, unknown, EstimateFormOutput>({
    resolver: zodResolver(estimateSchema),
    defaultValues: {
      customerId: "",
      propertyId: "",
      taxRate: 0,
      notes: "",
      rooms: [emptyRoom()],
    },
  });

  const {
    control,
    register,
    handleSubmit,
    watch,
    reset,
    setValue,
    formState: { errors, isSubmitting },
  } = methods;
  const { fields, append, remove } = useFieldArray({ control, name: "rooms" });

  const seededEstimateId = useRef<string | null>(null);

  useEffect(() => {
    if (existing.data && seededEstimateId.current !== existing.data.id) {
      seededEstimateId.current = existing.data.id;

      reset({
        customerId: existing.data.customerId,
        propertyId: existing.data.propertyId,
        taxRate: existing.data.taxRate,
        notes: existing.data.notes ?? "",
        rooms: existing.data.rooms.map((r) => ({
          id: r.id,
          name: r.name,
          lengthFeet: r.lengthFeet,
          widthFeet: r.widthFeet,
          wastePercentage: r.wastePercentage,
          flooringType: r.flooringType,
          workType: r.workType,
          installationMethod: r.installationMethod ?? UNSPECIFIED,
          finishType: r.finishType ?? UNSPECIFIED,
          notes: r.notes ?? "",
          laborRatePerSqFt: r.laborRatePerSqFt,
          materialRatePerSqFt: r.materialRatePerSqFt,
        })),
      });
    }
  }, [existing.data, reset]);

  const rooms = watch("rooms");
  const tax = Number(watch("taxRate")) || 0;
  const preview = estimateTotals(
    rooms.map((r) => ({
      lengthFeet: Number(r.lengthFeet) || 0,
      widthFeet: Number(r.widthFeet) || 0,
      wastePercentage: Number(r.wastePercentage) || 0,
      laborRatePerSqFt: Number(r.laborRatePerSqFt) || 0,
      materialRatePerSqFt: Number(r.materialRatePerSqFt) || 0,
    })),
    tax,
  );

  const customerId = watch("customerId");

  return (
    <FormProvider {...methods}>
      <form
        onSubmit={handleSubmit(async (values) => {
          try {
            const saved = isEdit ? await update.mutateAsync(values) : await create.mutateAsync(values);
            toast.success(isEdit ? "Draft saved" : "Estimate created");
            navigate(`/estimates/${saved.id}/edit`);
          } catch (err) {
            toast.error(err instanceof Error ? err.message : "Save failed");
          }
        })}
        className="grid grid-cols-1 lg:grid-cols-[1fr_20rem] gap-6"
      >
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>{isEdit ? `Estimate ${existing.data?.estimateNumber ?? ""}` : "New Estimate"}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm">Customer</label>
                  <CustomerPicker
                    value={customerId}
                    onChange={(id) => {
                      setValue("customerId", id);
                      setValue("propertyId", "");
                    }}
                  />
                  {errors.customerId && <p className="text-destructive text-sm">{errors.customerId.message}</p>}
                </div>
                <div>
                  <label className="text-sm">Property</label>
                  <PropertyPicker
                    customerId={customerId || undefined}
                    value={watch("propertyId")}
                    onChange={(id) => setValue("propertyId", id)}
                  />
                  {errors.propertyId && <p className="text-destructive text-sm">{errors.propertyId.message}</p>}
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm">Tax</label>
                  <Input type="number" step="0.1" {...register("taxRate")} />
                </div>
              </div>

              <div>
                <label className="text-sm">Notes</label>
                <Textarea rows={3} {...register("notes")} />
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle>Rooms</CardTitle>
              <Button type="button" onClick={() => append(emptyRoom())}>
                Add room
              </Button>
            </CardHeader>
            <CardContent className="space-y-4">
              {fields.map((f, i) => (
                <RoomRow key={f.id} index={i} onRemove={() => remove(i)} />
              ))}
              {errors.rooms && !Array.isArray(errors.rooms) && (
                <p className="text-destructive text-sm">{errors.rooms.message}</p>
              )}
            </CardContent>
          </Card>
        </div>

        <aside className="lg:sticky lg:top-4 h-fit">
          <Card>
            <CardHeader>
              <CardTitle>Summary</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2 text-sm">
              <Row label="Labor subtotal" value={fmt(preview.laborSubtotal)} />
              <Row label="Material subtotal" value={fmt(preview.materialSubtotal)} />
              <Row label="Subtotal" value={fmt(preview.subtotal)} bold />
              <Row label="Tax" value={fmt(preview.tax)} />
              <Row label="Total" value={fmt(preview.total)} bold />

              <Button type="submit" className="w-full mt-4" disabled={isSubmitting}>
                {isSubmitting ? "Saving…" : "Save draft"}
              </Button>
            </CardContent>
          </Card>
        </aside>
      </form>
    </FormProvider>
  );
}

function Row({ label, value, bold }: { label: string; value: string; bold?: boolean }) {
  return (
    <div className={`flex justify-between ${bold ? "font-semibold" : ""}`}>
      <span>{label}</span>
      <span>{value}</span>
    </div>
  );
}