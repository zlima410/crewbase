import { Component, type ErrorInfo, type ReactNode } from "react";
import { Button } from "./ui/button";

type Props = {
  children: ReactNode;
  resetKey?: string;
};

type State = { error: Error | null };

export class ErrorBoundary extends Component<Props, State> {
  state: State = { error: null };

  static getDerivedStateFromError(error: Error): State {
    return { error };
  }

  componentDidUpdate(previous: Props) {
    if (previous.resetKey !== this.props.resetKey && this.state.error) {
      this.setState({ error: null });
    }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    // Console only for now; wire to a real error reporter when one exists
    // (post-MVP backlog).
    console.error("Unhandled render error", error, info.componentStack);
  }

  render() {
    const { error } = this.state;

    if (!error) return this.props.children;

    return (
      <div className="mx-auto max-w-lg space-y-4 rounded-lg border p-6 text-center">
        <h1 className="text-lg font-semibold">Something went wrong</h1>
        <p className="text-muted-foreground text-sm">
          This screen failed to load. Your saved work is unaffected.
        </p>
        {import.meta.env.DEV && (
          <pre className="bg-muted overflow-auto rounded p-3 text-left text-xs">{error.message}</pre>
        )}
        <div className="flex justify-center gap-2">
          <Button variant="outline" onClick={() => this.setState({ error: null })}>
            Try again
          </Button>
          <Button onClick={() => window.location.reload()}>Reload app</Button>
        </div>
      </div>
    );
  }
}
