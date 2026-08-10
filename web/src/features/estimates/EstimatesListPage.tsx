import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { Button } from "../../components/ui/button";
import { Badge } from "../../components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "../../components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "../../components/ui/table";
import { Skeleton } from "../../components/ui/skeleton";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "../../components/ui/select";
import { useEstimates } from "../../api/estimates";
import type { EstimateStatus } from "../../api/types";
import { fmt } from "./pricingMath";

const PAGE_SIZE = 25;

const STATUS_OPTIONS: Array<{ value: "all" | EstimateStatus; label: string }> = [
  { value: "all", label: "All statuses" },
  { value: "Draft", label: "Draft" },
  { value: "Sent", label: "Sent" },
  { value: "Accepted", label: "Accepted" },
  { value: "Rejected", label: "Rejected" },
  { value: "Expired", label: "Expired" },
];

function statusVariant(status: EstimateStatus): "default" | "secondary" | "outline" | "destructive" {
  switch (status) {
    case "Draft":
      return "secondary";
    case "Sent":
      return "outline";
    case "Accepted":
      return "default";
    case "Rejected":
    case "Expired":
      return "destructive";
    default:
      return "secondary";
  }
}

export function EstimatesListPage() {
  const [params, setParams] = useSearchParams();
  const navigate = useNavigate();

  const page = Number(params.get("page") ?? 1);
  const statusParam = params.get("status");
  const status =
    statusParam && STATUS_OPTIONS.some((o) => o.value === statusParam) ? (statusParam as EstimateStatus) : undefined;

  const { data, isLoading, isFetching } = useEstimates(page, PAGE_SIZE, status);
  const total = data?.total ?? 0;
  const lastPage = Math.max(1, Math.ceil(total / PAGE_SIZE));

  const updateParams = (next: { page?: number; status?: "all" | EstimateStatus }) => {
    const pageValue = String(next.page ?? page);
    const statusValue = next.status ?? status ?? "all";

    if (statusValue === "all") {
      setParams({ page: pageValue }, { replace: true });
    } else {
      setParams({ page: pageValue, status: statusValue }, { replace: true });
    }
  };

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between gap-4">
        <CardTitle>Estimates</CardTitle>
        <div className="flex gap-2">
          <Select
            value={status ?? "all"}
            onValueChange={(value) => updateParams({ page: 1, status: value as "all" | EstimateStatus })}
          >
            <SelectTrigger className="w-44">
              <SelectValue placeholder="Filter status" />
            </SelectTrigger>
            <SelectContent>
              {STATUS_OPTIONS.map((option) => (
                <SelectItem key={option.value} value={option.value}>
                  {option.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Button asChild>
            <Link to="/estimates/new">New estimate</Link>
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <Skeleton className="h-64 w-full" />
        ) : total === 0 ? (
          <p className="text-muted-foreground py-8 text-center">
            {status ? "No estimates match this status." : "No estimates yet."}
          </p>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Number</TableHead>
                <TableHead>Customer</TableHead>
                <TableHead>Property</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="text-right">Total</TableHead>
                <TableHead>Created</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data!.items.map((estimate) => (
                <TableRow
                  key={estimate.id}
                  className="cursor-pointer"
                  onClick={() => navigate(`/estimates/${estimate.id}/edit`)}
                >
                  <TableCell>
                    <Link
                      to={`/estimates/${estimate.id}/edit`}
                      className="hover:underline"
                      onClick={(e) => e.stopPropagation()}
                    >
                      {estimate.estimateNumber}
                    </Link>
                  </TableCell>
                  <TableCell>{estimate.customerName}</TableCell>
                  <TableCell>{estimate.propertyAddress}</TableCell>
                  <TableCell>
                    <Badge variant={statusVariant(estimate.status)}>{estimate.status}</Badge>
                  </TableCell>
                  <TableCell className="text-right">{fmt(estimate.total)}</TableCell>
                  <TableCell>{new Date(estimate.createdDate).toLocaleDateString()}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}

        <div className="flex items-center justify-between mt-4 text-sm">
          <span className="text-muted-foreground">
            {total} estimate{total === 1 ? "" : "s"}
            {isFetching && " · updating…"}
          </span>
          <div className="flex items-center gap-2">
            <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => updateParams({ page: page - 1 })}>
              Previous
            </Button>
            <span>
              Page {page} of {lastPage}
            </span>
            <Button
              variant="outline"
              size="sm"
              disabled={page >= lastPage}
              onClick={() => updateParams({ page: page + 1 })}
            >
              Next
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}