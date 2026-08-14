import { Outlet } from "react-router-dom";
import { TopNav } from "./TopNav";

export function AppLayout() {
  return (
    <div className="min-h-screen bg-background text-foreground">
      <TopNav />
      <main className="max-w-6xl mx-auto p-4">
        <Outlet />
      </main>
    </div>
  );
}
