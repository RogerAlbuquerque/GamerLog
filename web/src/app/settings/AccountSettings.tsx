"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Field";
import { Modal } from "@/components/ui/Modal";
import { setCsrfToken } from "@/lib/api/client";
import { authApi } from "@/lib/api/endpoints";
import { applyServerError } from "@/lib/forms";
import { changePasswordSchema, type ChangePasswordForm } from "@/lib/schemas";
import { useSession } from "@/stores/session";
import { toast } from "@/stores/toast";

function PasswordSection() {
  const [formError, setFormError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<ChangePasswordForm>({ resolver: zodResolver(changePasswordSchema), defaultValues: { currentPassword: "", newPassword: "", confirmPassword: "" } });

  const onSubmit = handleSubmit(async (v) => {
    setFormError(null);
    try {
      const { csrfToken } = await authApi.changePassword({ currentPassword: v.currentPassword, newPassword: v.newPassword });
      setCsrfToken(csrfToken); // the session was re-issued, so the antiforgery token changed with it
      reset();
      toast.success("Password changed. Other devices have been signed out.");
    } catch (error) {
      setFormError(applyServerError(error, setError, ["currentPassword", "newPassword"]));
    }
  });

  return (
    <section className="surface space-y-4 p-6" aria-labelledby="password-heading">
      <h2 id="password-heading" className="text-xl font-bold">Password</h2>
      <form onSubmit={onSubmit} noValidate className="max-w-md space-y-4">
        {formError && <p role="alert" className="rounded-xl border border-danger/40 bg-danger/10 px-4 py-3 text-sm text-danger">{formError}</p>}
        <Input label="Current password" type="password" autoComplete="current-password" error={errors.currentPassword?.message} {...register("currentPassword")} />
        <Input label="New password" type="password" autoComplete="new-password" hint="At least 10 characters." error={errors.newPassword?.message} {...register("newPassword")} />
        <Input label="Confirm new password" type="password" autoComplete="new-password" error={errors.confirmPassword?.message} {...register("confirmPassword")} />
        <Button type="submit" loading={isSubmitting}>Change password</Button>
      </form>
    </section>
  );
}

function DeleteSection() {
  const router = useRouter();
  const clear = useSession((s) => s.clear);
  const [open, setOpen] = useState(false);
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | undefined>();
  const [busy, setBusy] = useState(false);

  const confirm = async () => {
    setBusy(true);
    setError(undefined);
    try {
      await authApi.deleteAccount(password);
      clear();
      toast.info("Your account has been deleted.");
      router.replace("/");
    } catch (e) {
      setError(e instanceof Error ? e.message : "Could not delete the account.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <section className="surface space-y-3 border-danger/30 p-6" aria-labelledby="delete-heading">
      <h2 id="delete-heading" className="text-xl font-bold text-danger">Delete account</h2>
      <p className="max-w-xl text-sm text-muted">Permanently removes your profile, diary, reviews, comments and collections. This cannot be undone.</p>
      <Button variant="danger" onClick={() => setOpen(true)}>Delete my account…</Button>
      <Modal open={open} onClose={() => setOpen(false)} title="Delete your account?">
        <div className="space-y-4">
          <p className="text-sm text-muted">Enter your password to confirm.</p>
          <Input label="Password" type="password" autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} error={error} />
          <div className="flex justify-end gap-3">
            <Button variant="ghost" onClick={() => setOpen(false)}>Cancel</Button>
            <Button variant="danger" loading={busy} disabled={!password} onClick={confirm}>Delete permanently</Button>
          </div>
        </div>
      </Modal>
    </section>
  );
}

export function AccountSettings() {
  return (
    <>
      <PasswordSection />
      <DeleteSection />
    </>
  );
}
