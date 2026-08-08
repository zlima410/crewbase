import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { Input } from '../../components/ui/input'
import { Button } from '../../components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/card'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../../components/ui/table'
import { Skeleton } from '../../components/ui/skeleton'
import { useCustomers } from '../../api/customers'
import { useDebouncedValue } from '../../lib/useDebouncedValue'

const PAGE_SIZE = 25;

export function CustomersPage() {
  const [params, setParams] = useSearchParams();
  const navigate = useNavigate();
  const page = Number(params.get("page") ?? 1);
  const [search, setSearch] = useState(params.get("search") ?? "");
  const debounced = useDebouncedValue(search, 250);

  const { data, isLoading, isFetching } = useCustomers(debounced, page, PAGE_SIZE);
  const total = data?.total ?? 0;
  const lastPage = Math.max(1, Math.ceil(total / PAGE_SIZE));

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between gap-4">
        <CardTitle>Customers</CardTitle>
        <div className="flex gap-2">
          <Input
            placeholder="Search name, email, phone…"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setParams({ search: e.target.value, page: "1" }, { replace: true });
            }}
            className="w-72"
          />
          <Button asChild>
            <Link to="/customers/new">Add Customer</Link>
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <Skeleton className="h-64 w-full" />
        ) : total === 0 ? (
          <p className="text-muted-foreground py-8 text-center">
            {debounced ? "No customers match your search." : "No customers yet."}
          </p>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Phone</TableHead>
                <TableHead>Email</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data!.items.map((c) => (
                <TableRow key={c.id} className="cursor-pointer" onClick={() => navigate(`/customers/${c.id}`)}>
                  <TableCell>
                    <Link to={`/customers/${c.id}`} className="hover:underline" onClick={(e) => e.stopPropagation()}>
                      {c.lastName}, {c.firstName}
                    </Link>
                  </TableCell>
                  <TableCell>{c.phone}</TableCell>
                  <TableCell>{c.email ?? "—"}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}

        <div className="flex items-center justify-between mt-4 text-sm">
          <span className="text-muted-foreground">
            {total} customer{total === 1 ? "" : "s"}
            {isFetching && " · updating…"}
          </span>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={page <= 1}
              onClick={() => setParams({ search: debounced, page: String(page - 1) })}
            >
              Previous
            </Button>
            <span>
              Page {page} of {lastPage}
            </span>
            <Button
              variant="outline"
              size="sm"
              disabled={page >= lastPage}
              onClick={() => setParams({ search: debounced, page: String(page + 1) })}
            >
              Next
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}