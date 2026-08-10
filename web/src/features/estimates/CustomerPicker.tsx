import { useCustomers } from '../../api/customers'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../../components/ui/select'

export function CustomerPicker({ value, onChange }: { value: string | undefined; onChange: (id: string) => void }) {
  const { data } = useCustomers("", 1, 100);
  return (
    <Select value={value} onValueChange={onChange}>
      <SelectTrigger>
        <SelectValue placeholder="Select customer" />
      </SelectTrigger>
      <SelectContent>
        {data?.items.map((c) => (
          <SelectItem key={c.id} value={c.id}>
            {c.lastName}, {c.firstName}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}
