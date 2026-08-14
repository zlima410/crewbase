import { Input } from "../../components/ui/input";
import { Textarea } from "../../components/ui/textarea";
import { Button } from "../../components/ui/button";
import { Label } from "../../components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "../../components/ui/select";
import { UNSPECIFIED, type EstimateFormValues } from "./estimateSchema";
import { roomPreview, fmt } from "./pricingMath";
import { Trash2 } from "lucide-react";
import { useFormContext, Controller } from "react-hook-form";
import type { ReactNode } from "react";

export function RoomRow({ index, onRemove }: { index: number; onRemove: () => void }) {
  const { register, watch, formState } = useFormContext<EstimateFormValues>();
  const row = watch(`rooms.${index}`);
  const errs = formState.errors.rooms?.[index];

  const { billableSquareFeet, laborCost, materialCost, roomTotal } = roomPreview({
    lengthFeet: Number(row?.lengthFeet) || 0,
    widthFeet: Number(row?.widthFeet) || 0,
    wastePercentage: Number(row?.wastePercentage) || 0,
    laborRatePerSqFt: Number(row?.laborRatePerSqFt) || 0,
    materialRatePerSqFt: Number(row?.materialRatePerSqFt) || 0,
  });

  return (
    <div className="border rounded-md p-4 space-y-3">
      <div className="flex items-center gap-2">
        <Input {...register(`rooms.${index}.name`)} placeholder="Room name (e.g., Living Room)" />
        <Button type="button" variant="ghost" size="icon" onClick={onRemove}>
          <Trash2 className="w-4 h-4" />
        </Button>
      </div>

      <div className="grid grid-cols-3 gap-3">
        <NumField label="Length (ft)" name={`rooms.${index}.lengthFeet`} error={errs?.lengthFeet?.message} />
        <NumField label="Width (ft)" name={`rooms.${index}.widthFeet`} error={errs?.widthFeet?.message} />
        <NumField label="Waste %" name={`rooms.${index}.wastePercentage`} error={errs?.wastePercentage?.message} />
      </div>

      <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
        <Field label="Work type">
          <SelectField
            name={`rooms.${index}.workType`}
            options={[
              ["NewInstallation", "New installation"],
              ["Refinishing", "Refinishing"],
              ["Repair", "Repair"],
              ["ScreenAndRecoat", "Screen & recoat"],
              ["Removal", "Removal"],
            ]}
          />
        </Field>
        <Field label="Flooring type">
          <SelectField
            name={`rooms.${index}.flooringType`}
            options={[
              ["SolidHardwood", "Solid hardwood"],
              ["EngineeredHardwood", "Engineered hardwood"],
              ["ExistingHardwood", "Existing hardwood"],
              ["Other", "Other"],
            ]}
          />
        </Field>
        <Field label="Installation method">
          <SelectField
            name={`rooms.${index}.installationMethod`}
            options={[
              [UNSPECIFIED, "Not specified"],
              ["NailDown", "Nail down"],
              ["GlueDown", "Glue down"],
              ["Floating", "Floating"],
              ["Existing", "Existing floor"],
              ["Unknown", "Unknown"],
            ]}
          />
        </Field>
        <Field label="Finish type">
          <SelectField
            name={`rooms.${index}.finishType`}
            options={[
              [UNSPECIFIED, "Not specified"],
              ["WaterBased", "Water based"],
              ["OilBased", "Oil based"],
              ["Unfinished", "Unfinished"],
              ["PreFinished", "Pre-finished"],
              ["Other", "Other"],
            ]}
          />
        </Field>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <NumField
          label="Labor rate / sq ft"
          name={`rooms.${index}.laborRatePerSqFt`}
          error={errs?.laborRatePerSqFt?.message}
        />
        <NumField
          label="Material rate / sq ft"
          name={`rooms.${index}.materialRatePerSqFt`}
          error={errs?.materialRatePerSqFt?.message}
        />
      </div>

      <Field label="Room notes" error={errs?.notes?.message}>
        <Textarea
          rows={2}
          placeholder="Stain, product line, or anything the fields above don't cover"
          {...register(`rooms.${index}.notes`)}
        />
      </Field>

      <div className="grid grid-cols-4 text-sm bg-muted/40 rounded p-2">
        <Stat label="Billable" value={`${billableSquareFeet.toFixed(2)} sq ft`} />
        <Stat label="Labor" value={fmt(laborCost)} />
        <Stat label="Material" value={fmt(materialCost)} />
        <Stat label="Room total" value={fmt(roomTotal)} bold />
      </div>
    </div>
  );
}

function Field({ label, error, children }: { label: string; error?: string; children: ReactNode }) {
  return (
    <div className="space-y-1">
      <Label className="text-xs">{label}</Label>
      {children}
      {error && <p className="text-destructive text-xs">{error}</p>}
    </div>
  );
}

function NumField({
  label,
  name,
  error,
}: {
  label: string;
  name: `rooms.${number}.${
    | "lengthFeet"
    | "widthFeet"
    | "wastePercentage"
    | "laborRatePerSqFt"
    | "materialRatePerSqFt"}`;
  error?: string;
}) {
  const { register } = useFormContext<EstimateFormValues>();
  return (
    <Field label={label} error={error}>
      <Input type="number" step="0.01" inputMode="decimal" {...register(name, { valueAsNumber: true })} />
    </Field>
  );
}

function SelectField({
  name,
  options,
}: {
  name: `rooms.${number}.${"workType" | "flooringType" | "installationMethod" | "finishType"}`;
  options: [string, string][];
}) {
  const { control } = useFormContext<EstimateFormValues>();
  return (
    <Controller
      control={control}
      name={name}
      render={({ field }) => (
        <Select value={field.value} onValueChange={field.onChange}>
          <SelectTrigger className="w-full min-w-0">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {options.map(([value, label]) => (
              <SelectItem key={value} value={value}>
                {label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      )}
    />
  );
}

function Stat({ label, value, bold }: { label: string; value: string; bold?: boolean }) {
  return (
    <div className="flex flex-col">
      <span className="text-muted-foreground text-xs">{label}</span>
      <span className={bold ? "font-semibold" : ""}>{value}</span>
    </div>
  );
}