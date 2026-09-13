import { Link, NavLink } from "react-router-dom";
import { Button } from "../ui/button";
import { useAuth } from "../../auth/useAuth";
import { cn } from "../../lib/utils";

const links = [
  { to: "/customers", label: "Customers" },
  { to: "/estimates", label: "Estimates" },
  { to: "/jobs", label: "Jobs" },
] as const;

export function TopNav() {
  const { signOut } = useAuth();

  return (
    <header className="border-b">
      <div className="max-w-6xl mx-auto flex items-center justify-between p-4">
        <Link to="/customers" className="font-semibold">
          Crewbase
        </Link>
        <nav className="flex items-center gap-4">
          {links.map((link) => (
            <NavLink
              key={link.to}
              to={link.to}
              className={({ isActive }) =>
                cn("text-sm hover:underline", isActive && "font-medium underline underline-offset-4")
              }
            >
              {link.label}
            </NavLink>
          ))}
          <Button variant="ghost" size="sm" onClick={signOut}>
            Sign out
          </Button>
        </nav>
      </div>
    </header>
  );
}