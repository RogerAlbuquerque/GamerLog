import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@/lib/api/client";

const replace = vi.fn();
let nextParam: string | null = null;
vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push: vi.fn() }),
  useSearchParams: () => new URLSearchParams(nextParam ? { next: nextParam } : {}),
}));
vi.mock("next/link", () => ({ default: ({ href, children, ...rest }: { href: string; children: React.ReactNode }) => <a href={href} {...rest}>{children}</a> }));

import { useSession } from "@/stores/session";
import { LoginForm } from "./LoginForm";

const login = vi.fn();

beforeEach(() => {
  vi.clearAllMocks();
  nextParam = null;
  useSession.setState({ login } as never);
});

describe("LoginForm", () => {
  it("validates required fields before calling the API", async () => {
    render(<LoginForm />);
    await userEvent.click(screen.getByRole("button", { name: "Sign in" }));
    expect(await screen.findByText("Enter your username or email.")).toBeInTheDocument();
    expect(screen.getByText("Enter your password.")).toBeInTheDocument();
    expect(login).not.toHaveBeenCalled();
  });

  it("signs in and returns to the requested page", async () => {
    nextParam = "/diary";
    login.mockResolvedValue({ displayName: "Roger" });
    render(<LoginForm />);
    await userEvent.type(screen.getByLabelText("Username or email"), "roger");
    await userEvent.type(screen.getByLabelText("Password"), "correct-horse-9");
    await userEvent.click(screen.getByRole("button", { name: "Sign in" }));
    await waitFor(() => expect(replace).toHaveBeenCalledWith("/diary"));
    expect(login).toHaveBeenCalledWith("roger", "correct-horse-9");
  });

  it("ignores an unsafe next parameter", async () => {
    nextParam = "https://evil.example";
    login.mockResolvedValue({ displayName: "Roger" });
    render(<LoginForm />);
    await userEvent.type(screen.getByLabelText("Username or email"), "roger");
    await userEvent.type(screen.getByLabelText("Password"), "x");
    await userEvent.click(screen.getByRole("button", { name: "Sign in" }));
    await waitFor(() => expect(replace).toHaveBeenCalledWith("/"));
  });

  it("shows the server's message when credentials are wrong", async () => {
    login.mockRejectedValue(new ApiError(401, "Invalid username or password."));
    render(<LoginForm />);
    await userEvent.type(screen.getByLabelText("Username or email"), "roger");
    await userEvent.type(screen.getByLabelText("Password"), "nope");
    await userEvent.click(screen.getByRole("button", { name: "Sign in" }));
    expect(await screen.findByRole("alert")).toHaveTextContent("Invalid username or password.");
    expect(replace).not.toHaveBeenCalled();
  });
});
