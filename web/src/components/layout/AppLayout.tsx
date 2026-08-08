import { Link, Outlet } from 'react-router-dom'
import { Button } from '../../components/ui/button'
import { useAuth } from '../../auth/useAuth'

export function AppLayout() {
  const { signOut } = useAuth();
  return (
    <div className="min-h-screen bg-background text-foreground">
      <header className="border-b">
        <div className="max-w-6xl mx-auto flex items-center justify-between p-4">
          <Link to="/customers" className="font-semibold">
            Crewbase
          </Link>
          <nav className="flex items-center gap-4">
            <Link to="/customers" className="text-sm hover:underline">
              Customers
            </Link>
            <Button variant="ghost" size="sm" onClick={signOut}>
              Sign out
            </Button>
          </nav>
        </div>
      </header>
      <main className="max-w-6xl mx-auto p-4">
        <Outlet />
      </main>
    </div>
  );
}
