import { Outlet, useLocation } from "react-router-dom";
import { ErrorBoundary } from "../ErrorBoundary";
import { TopNav } from "./TopNav";

export function AppLayout() {
  const location = useLocation();

  return (
    <div className="min-h-screen bg-background text-foreground">
      <TopNav />
      <main className="max-w-6xl mx-auto p-4">
        <ErrorBoundary resetKey={location.pathname}>
          <Outlet />
        </ErrorBoundary>
      </main>
    </div>
  );
}
