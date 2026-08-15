import { useState } from "react";
import { useSearchParams } from "react-router-dom";
import { Button } from "../../components/ui/button";
import { Input } from "../../components/ui/input";
import { Card, CardContent, CardHeader, CardTitle } from "../../components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "../../components/ui/table";
import { Skeleton } from "../../components/ui/skeleton";
import { useJobs } from "../../api/jobs";
import { useDebouncedValue } from "../../lib/useDebouncedValue";
import type { JobListItem, JobStatus } from "../../api/types";

const PAGE_SIZE = 25;
const BOARD_SIZE = 10;

const STATUS_FILTERS: Array<{ value: "all" | JobStatus; label: string }> = [
  { value: "all", label: "All" },
  { value: "Scheduled", label: "Scheduled" },
  { value: "InProgress", label: "In Progress" },
  { value: "Completed", label: "Completed" },
  { value: "Waiting", label: "Waiting" },
  { value: "Cancelled", label: "Cancelled" },
];

function statusLabel(status: JobStatus): string {
  return status === "InProgress" ? "In Progress" : status;
}

function formatDate(value: string | null): string {
  if (!value) return "—";
  return new Date(value).toLocaleDateString();
}

function emptyCopy(status: JobStatus, searching: boolean): string {
  if (searching) return `No ${statusLabel(status).toLowerCase()} jobs match this search.`;
  switch (status) {
    case "Scheduled":
      return "No scheduled jobs.";
    case "InProgress":
      return "No jobs in progress.";
    case "Completed":
      return "No completed jobs.";
    default:
      return `No ${statusLabel(status).toLowerCase()} jobs.`;
  }
}

export function JobsListPage() {
  const [params, setParams] = useSearchParams();
  const page = Number(params.get("page") ?? 1);
  const statusParam = params.get("status");
  const status = STATUS_FILTERS.some((o) => o.value === statusParam && o.value !== "all")
    ? (statusParam as JobStatus)
    : undefined;

  const [search, setSearch] = useState(params.get("search") ?? "");
  const debounced = useDebouncedValue(search, 250);
  const isAll = !status;

  const scheduled = useJobs(1, BOARD_SIZE, "Scheduled", debounced, isAll);
  const inProgress = useJobs(1, BOARD_SIZE, "InProgress", debounced, isAll);
  const completed = useJobs(1, BOARD_SIZE, "Completed", debounced, isAll);
  const waiting = useJobs(1, BOARD_SIZE, "Waiting", debounced, isAll);
  const cancelled = useJobs(1, BOARD_SIZE, "Cancelled", debounced, isAll);
  const filtered = useJobs(page, PAGE_SIZE, status, debounced, !isAll);

  const updateParams = (next: { page?: number; status?: "all" | JobStatus; search?: string }) => {
    const pageValue = String(next.page ?? page);
    const statusValue = next.status ?? status ?? "all";
    const searchValue = next.search ?? search;
    const nextParams: Record<string, string> = { page: pageValue };
    if (statusValue !== "all") nextParams.status = statusValue;
    if (searchValue) nextParams.search = searchValue;
    setParams(nextParams, { replace: true });
  };

  const board = [
    { status: "Scheduled" as const, query: scheduled },
    { status: "InProgress" as const, query: inProgress },
    { status: "Completed" as const, query: completed },
  ];
  const extras = [
    { status: "Waiting" as const, query: waiting },
    { status: "Cancelled" as const, query: cancelled },
  ].filter((section) => (section.query.data?.total ?? 0) > 0);

  const loading = isAll
    ? scheduled.isLoading || inProgress.isLoading || completed.isLoading || waiting.isLoading || cancelled.isLoading
    : filtered.isLoading;
  const fetching = isAll
    ? scheduled.isFetching || inProgress.isFetching || completed.isFetching || waiting.isFetching || cancelled.isFetching
    : filtered.isFetching;

  const filteredTotal = filtered.data?.total ?? 0;
  const lastPage = Math.max(1, Math.ceil(filteredTotal / PAGE_SIZE));
  const boardTotal =
    (scheduled.data?.total ?? 0) +
    (inProgress.data?.total ?? 0) +
    (completed.data?.total ?? 0) +
    (waiting.data?.total ?? 0) +
    (cancelled.data?.total ?? 0);

  return (
    <Card>
      <CardHeader className="gap-4">
        <CardTitle>Jobs</CardTitle>
        <div className="flex flex-wrap items-center gap-2">
          {STATUS_FILTERS.map((option) => {
            const selected = (status ?? "all") === option.value;
            return (
              <Button
                key={option.value}
                type="button"
                size="sm"
                variant={selected ? "default" : "outline"}
                onClick={() => updateParams({ page: 1, status: option.value })}
              >
                {option.label}
              </Button>
            );
          })}
          <Input
            placeholder="Search job #, customer, address…"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              updateParams({ page: 1, search: e.target.value });
            }}
            className="w-72"
          />
        </div>
      </CardHeader>
      <CardContent>
        {loading ? (
          <Skeleton className="h-64 w-full" />
        ) : isAll ? (
          boardTotal === 0 && debounced ? (
            <p className="text-muted-foreground py-8 text-center">No jobs match this search.</p>
          ) : (
            <div className="space-y-8">
              {[...board, ...extras].map((section) => (
                <JobSection
                  key={section.status}
                  status={section.status}
                  items={section.query.data?.items ?? []}
                  total={section.query.data?.total ?? 0}
                  searching={Boolean(debounced)}
                  onViewAll={() => updateParams({ page: 1, status: section.status })}
                />
              ))}
            </div>
          )
        ) : filteredTotal === 0 ? (
          <p className="text-muted-foreground py-8 text-center">
            {debounced ? "No jobs match this search." : emptyCopy(status, false)}
          </p>
        ) : (
          <JobsTable items={filtered.data!.items} />
        )}

        {!isAll && (
          <div className="flex items-center justify-between mt-4 text-sm">
            <span className="text-muted-foreground">
              {filteredTotal} job{filteredTotal === 1 ? "" : "s"}
              {fetching && " · updating…"}
            </span>
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                disabled={page <= 1}
                onClick={() => updateParams({ page: page - 1 })}
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
                onClick={() => updateParams({ page: page + 1 })}
              >
                Next
              </Button>
            </div>
          </div>
        )}

        {isAll && (
          <div className="mt-4 text-sm text-muted-foreground">
            {boardTotal} job{boardTotal === 1 ? "" : "s"}
            {fetching && " · updating…"}
          </div>
        )}
      </CardContent>
    </Card>
  );
}

function JobSection({
  status,
  items,
  total,
  searching,
  onViewAll,
}: {
  status: JobStatus;
  items: JobListItem[];
  total: number;
  searching: boolean;
  onViewAll: () => void;
}) {
  return (
    <section>
      <div className="mb-3 flex items-center justify-between gap-2">
        <h2 className="font-medium">
          {statusLabel(status)}
          <span className="text-muted-foreground font-normal"> · {total}</span>
        </h2>
        {total > items.length && (
          <Button type="button" variant="ghost" size="sm" onClick={onViewAll}>
            View all
          </Button>
        )}
      </div>
      {items.length === 0 ? (
        <p className="text-muted-foreground py-4 text-sm">{emptyCopy(status, searching)}</p>
      ) : (
        <JobsTable items={items} />
      )}
    </section>
  );
}

function JobsTable({ items }: { items: JobListItem[] }) {
  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Number</TableHead>
          <TableHead>Customer</TableHead>
          <TableHead>Property</TableHead>
          <TableHead>Scheduled</TableHead>
          <TableHead>Created</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {items.map((job) => (
          <TableRow key={job.id}>
            <TableCell>{job.jobNumber}</TableCell>
            <TableCell>{job.customerName}</TableCell>
            <TableCell>{job.propertyAddress}</TableCell>
            <TableCell>{formatDate(job.scheduledStart)}</TableCell>
            <TableCell>{formatDate(job.createdAt)}</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
