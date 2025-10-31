"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { AuthShell } from "@/components/forms/AuthShell";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Field";
import { applyServerError } from "@/lib/forms";
import { registerSchema, type RegisterForm as Values } from "@/lib/schemas";
import { useSession } from "@/stores/session";
import { toast } from "@/stores/toast";

const FIELDS = ["username", "email", "displayName", "password"] as const;

export function RegisterForm() {
  const router = useRouter();
  const registerUser = useSession((s) => s.register);
  const [formError, setFormError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<Values>({
    resolver: zodResolver(registerSchema),
    defaultValues: { username: "", email: "", displayName: "", password: "", confirmPassword: "" },
  });

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);
    try {
      const user = await registerUser({
        username: values.username,
        email: values.email,
        password: values.password,
        displayName: values.displayName || undefined,
      });
      toast.success(`Welcome to GameLog, ${user.displayName}!`);
      router.replace("/discover");
    } catch (error) {
      setFormError(applyServerError(error, setError, FIELDS));
    }
  });

  return (
    <AuthShell
      title="Create your account"
      subtitle="Free forever. Start logging in under a minute."
      footer={
        <>
          Already have an account?{" "}
          <Link href="/login" className="font-semibold text-accent-soft hover:underline">
            Sign in
          </Link>
        </>
      }
    >
      <form onSubmit={onSubmit} noValidate className="space-y-4">
        {formError && (
          <p role="alert" className="rounded-xl border border-danger/40 bg-danger/10 px-4 py-3 text-sm text-danger">
            {formError}
          </p>
        )}
        <Input label="Username" autoComplete="username" hint="3-24 characters: letters, digits or underscore." error={errors.username?.message} {...register("username")} />
        <Input label="Email" type="email" autoComplete="email" error={errors.email?.message} {...register("email")} />
        <Input label="Display name (optional)" autoComplete="nickname" error={errors.displayName?.message} {...register("displayName")} />
        <Input label="Password" type="password" autoComplete="new-password" hint="At least 10 characters. Length beats complexity: a short sentence works well." error={errors.password?.message} {...register("password")} />
        <Input label="Confirm password" type="password" autoComplete="new-password" error={errors.confirmPassword?.message} {...register("confirmPassword")} />
        <Button type="submit" loading={isSubmitting} className="w-full" size="lg">
          Create account
        </Button>
      </form>
    </AuthShell>
  );
}
