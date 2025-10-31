import type { FieldValues, Path, UseFormSetError } from "react-hook-form";
import { ApiError } from "@/lib/api/client";

/**
 * Pushes server-side validation messages onto the matching form fields. Returns the message to show above the
 * form when the error is not about a specific field (wrong password, rate limit, network trouble...).
 */
export function applyServerError<T extends FieldValues>(error: unknown, setError: UseFormSetError<T>, knownFields: readonly string[]): string | null {
  if (!(error instanceof ApiError)) return "Could not reach the server. Check your connection and try again.";
  let matched = false;
  for (const [field, message] of Object.entries(error.fieldErrors)) {
    if (knownFields.includes(field)) {
      setError(field as Path<T>, { type: "server", message });
      matched = true;
    }
  }
  return matched ? null : error.message;
}
