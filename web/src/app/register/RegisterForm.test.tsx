import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@/lib/api/client";

const replace = vi.fn();
vi.mock("next/navigation", () => ({ useRouter: () => ({ replace, push: vi.fn() }) }));
vi.mock("next/link", () => ({ default: ({ href, children, ...rest }: { href: string; children: React.ReactNode }) => <a href={href} {...rest}>{children}</a> }));

import { useSession } from "@/stores/session";
import { RegisterForm } from "./RegisterForm";

const register = vi.fn();

async function fill(overrides: Partial<Record<"Username" | "Email" | "Password" | "Confirm password", string>> = {}) {
  const v = { Username: "roger_a", Email: "roger@example.com", Password: "correct-horse-9", "Confirm password": "correct-horse-9", ...overrides };
  for (const [label, value] of Object.entries(v)) await userEvent.type(screen.getByLabelText(label), value);
  await userEvent.click(screen.getByRole("button", { name: "Create account" }));
}

beforeEach(() => {
  vi.clearAllMocks();
  useSession.setState({ register } as never);
});

describe("RegisterForm", () => {
  it("blocks mismatched passwords client-side", async () => {
    render(<RegisterForm />);
    await fill({ "Confirm password": "different-pass-1" });
    expect(await screen.findByText("Passwords do not match.")).toBeInTheDocument();
    expect(register).not.toHaveBeenCalled();
  });

  it("registers and goes to discovery", async () => {
    register.mockResolvedValue({ displayName: "roger_a" });
    render(<RegisterForm />);
    await fill();
    await waitFor(() => expect(replace).toHaveBeenCalledWith("/discover"));
    expect(register).toHaveBeenCalledWith({ username: "roger_a", email: "roger@example.com", password: "correct-horse-9", displayName: undefined });
  });

  it("shows server field errors next to the field", async () => {
    register.mockRejectedValue(new ApiError(409, "Taken", "username_taken", { username: "This username is already taken." }));
    render(<RegisterForm />);
    await fill();
    expect(await screen.findByText("This username is already taken.")).toBeInTheDocument();
    expect(replace).not.toHaveBeenCalled();
  });
});
