import { useProperties } from '../../api/properties'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../../components/ui/select'

export function PropertyPicker({
  customerId,
  value,
  onChange,
}: {
  customerId: string | undefined;
  value: string | undefined;
  onChange: (id: string) => void;
}) {
  const { data } = useProperties(customerId);
  return (
    <Select value={value} onValueChange={onChange} disabled={!customerId}>
      <SelectTrigger>
        <SelectValue placeholder={customerId ? "Select property" : "Select customer first"} />
      </SelectTrigger>
      <SelectContent>
        {(data ?? []).map((p) => (
          <SelectItem key={p.id} value={p.id}>
            {p.streetAddress} — {p.city}, {p.state}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}
