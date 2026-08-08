import { Link, useLocation } from "react-router-dom";
import { Button } from "../components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "../components/ui/card";
import { useAuth } from "../auth/useAuth";

export function NotFoundPage() {
  const { pathname } = useLocation();
  const { session } = useAuth();
  const homePath = session ? "/customers" : "/login";
  const homeLabel = session ? "Back to customers" : "Go to sign in";

  return (
    <div className="min-h-screen grid place-items-center p-4">
      <Card className="w-full max-w-md text-center">
        <CardHeader>
          <CardTitle className="text-3xl">404</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <p className="text-muted-foreground">
            We couldn&apos;t find <code className="rounded bg-muted px-1.5 py-0.5 text-sm">{pathname}</code>.
          </p>
          <Button asChild className="w-full">
            <Link to={homePath}>{homeLabel}</Link>
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}