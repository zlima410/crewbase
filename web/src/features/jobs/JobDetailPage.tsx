import { Link, useParams } from "react-router-dom";
import type { ReactNode } from "react";
import { Button } from "../../components/ui/button";
import { Badge } from "../../components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "../../components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "../../components/ui/table";
import { Skeleton } from "../../components/ui/skeleton";
import { useJob } from "../../api/jobs";
import type {
  FinishType,
  FlooringType,
  InstallationMethod,
  Job,
  JobRoom,
  JobStatus,
  WorkType,
} from "../../api/types";

function statusLabel(status: JobStatus): string {
  return status === "InProgress" ? "In Progress" : status;
}

function statusVariant(status: JobStatus): "default" | "secondary" | "outline" | "destructive" {
  switch (status) {
    case "Scheduled":
      return "outline";
    case "InProgress":
      return "default";
    case "Waiting":
      return "secondary";
    case "Completed":
      return "secondary";
    case "Cancelled":
      return "destructive";
    default:
      return "secondary";
  }
}

function workTypeLabel(value: WorkType): string {
  switch (value) {
    case "NewInstallation":
      return "New installation";
    case "ScreenAndRecoat":
      return "Screen & recoat";
    default:
      return value;
  }
}

function flooringLabel(value: FlooringType): string {
  switch (value) {
    case "SolidHardwood":
      return "Solid hardwood";
    case "EngineeredHardwood":
      return "Engineered hardwood";
    case "ExistingHardwood":
      return "Existing hardwood";
    default:
      return value;
  }
}

function installationLabel(value: InstallationMethod | null): string {
  switch (value) {
    case "NailDown":
      return "Nail down";
    case "GlueDown":
      return "Glue down";
    case "Floating":
      return "Floating";
    case "Existing":
      return "Existing floor";
    case "Unknown":
      return "Unknown";
    default:
      return "Not specified";
  }
}

function finishLabel(value: FinishType | null): string {
  switch (value) {
    case "WaterBased":
      return "Water based";
    case "OilBased":
      return "Oil based";
    case "Unfinished":
      return "Unfinished";
    case "PreFinished":
      return "Pre-finished";
    case "Other":
      return "Other";
    default:
      return "Not specified";
  }
}

function formatDate(value: string | null): string {
  if (!value) return "Not set";
  return new Date(value).toLocaleString();
}

function formatSqFt(value: number): string {
  return `${value.toLocaleString(undefined, { maximumFractionDigits: 2 })} sq ft`;
}

export function JobDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data, isLoading, isError } = useJob(id);

  if (isLoading) return <Skeleton className="h-64 w-full" />;
  if (isError || !data) return <p>Job not found.</p>;

  return <JobDetail job={data} />;
}

function JobDetail({ job }: { job: Job }) {
  const rooms = [...job.rooms].sort((a, b) => a.position - b.position);

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader className="flex flex-row items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <CardTitle>{job.jobNumber}</CardTitle>
            <Badge variant={statusVariant(job.status)}>{statusLabel(job.status)}</Badge>
          </div>
          <div className="flex gap-2">
            <Button variant="outline" asChild>
              <Link to="/jobs">Back</Link>
            </Button>
            <Button variant="outline" asChild>
              <Link to={`/estimates/${job.estimateId}/edit`}>View estimate</Link>
            </Button>
          </div>
        </CardHeader>
      </Card>

      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Customer</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <Info label="Name">
              <Link to={`/customers/${job.customerId}`} className="hover:underline">
                {job.customerName}
              </Link>
            </Info>
            <Info label="Phone">
              <a href={`tel:${job.customerPhone}`} className="hover:underline">
                {job.customerPhone}
              </a>
            </Info>
            <Info label="Email">
              {job.customerEmail ? (
                <a href={`mailto:${job.customerEmail}`} className="hover:underline">
                  {job.customerEmail}
                </a>
              ) : (
                "—"
              )}
            </Info>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Address</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <Info label="Property" value={job.propertyAddress} />
            <Info label="Access" value={job.propertyAccessNotes ?? "—"} />
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Schedule</CardTitle>
        </CardHeader>
        <CardContent className="grid gap-3 sm:grid-cols-2">
          <Info label="Scheduled start" value={formatDate(job.scheduledStart)} />
          <Info label="Scheduled end" value={formatDate(job.scheduledEnd)} />
          <Info label="Actual start" value={formatDate(job.actualStart)} />
          <Info label="Actual end" value={formatDate(job.actualEnd)} />
          <Info label="Created" value={formatDate(job.createdAt)} />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Rooms</CardTitle>
        </CardHeader>
        <CardContent>
          {rooms.length === 0 ? (
            <p className="text-muted-foreground">No rooms on this job.</p>
          ) : (
            <RoomsTable rooms={rooms} />
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Notes</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          <Info label="Description" value={job.description ?? "—"} />
          <Info label="Internal" value={job.internalNotes ?? "—"} />
          <Info label="Customer" value={job.customerNotes ?? "—"} />
          <div className="pt-2">
            <p className="text-muted-foreground text-sm">
              No field notes yet. Crew notes will appear here in order.
            </p>
          </div>
        </CardContent>
      </Card>

      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Assignments</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-muted-foreground text-sm">No one is assigned to this job yet.</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Photos</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-muted-foreground text-sm">No photos uploaded yet.</p>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

function RoomsTable({ rooms }: { rooms: JobRoom[] }) {
  return (
    <div className="overflow-x-auto">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Room</TableHead>
            <TableHead>Area</TableHead>
            <TableHead>Billable</TableHead>
            <TableHead>Work</TableHead>
            <TableHead>Flooring</TableHead>
            <TableHead>Install</TableHead>
            <TableHead>Finish</TableHead>
            <TableHead>Notes</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {rooms.map((room) => (
            <TableRow key={room.id}>
              <TableCell>{room.name}</TableCell>
              <TableCell>{formatSqFt(room.squareFeet)}</TableCell>
              <TableCell>{formatSqFt(room.billableSquareFeet)}</TableCell>
              <TableCell>{workTypeLabel(room.workType)}</TableCell>
              <TableCell>{flooringLabel(room.flooringType)}</TableCell>
              <TableCell>{installationLabel(room.installationMethod)}</TableCell>
              <TableCell>{finishLabel(room.finishType)}</TableCell>
              <TableCell>{room.notes ?? "—"}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}

function Info({
  label,
  value,
  children,
}: {
  label: string;
  value?: string;
  children?: ReactNode;
}) {
  return (
    <div className="grid grid-cols-[8rem_1fr] gap-4">
      <span className="text-muted-foreground text-sm">{label}</span>
      <span className="whitespace-pre-wrap">{children ?? value}</span>
    </div>
  );
}
