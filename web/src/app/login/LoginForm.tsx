"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { AuthShell } from "@/components/forms/AuthShell";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Field";
import { applyServerError } from "@/lib/forms";
import { loginSchema, type LoginForm as Values } from "@/lib/schemas";
import { safeNext } from "@/hooks/useRequireAuth";
import { useSession } from "@/stores/session";
import { toast } from "@/stores/toast";

export function LoginForm() {
  const router = useRouter();
  const next = safeNext(useSearchParams().get("next"));
  const login = useSession((s) => s.login);
  const [formError, setFormError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<Values>({ resolver: zodResolver(loginSchema), defaultValues: { login: "", password: "" } });

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);
    try {
      const user = await login(values.login, values.password);
      toast.success(`Welcome back, ${user.displayName}!`);
      router.replace(next);
    } catch (error) {
      setFormError(applyServerError(error, setError, ["login", "password"]));
    }
  });

  return (
    <AuthShell
      title="Welcome back"
      subtitle="Sign in to pick up your diary where you left it."
      footer={
        <>
          New to GameLog?{" "}
          <Link href="/register" className="font-semibold text-accent-soft hover:underline">
            Create an account
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
        <Input label="Username or email" autoComplete="username" autoFocus error={errors.login?.message} {...register("login")} />
        <Input label="Password" type="password" autoComplete="current-password" error={errors.password?.message} {...register("password")} />
        <Button type="submit" loading={isSubmitting} className="w-full" size="lg">
          Sign in
        </Button>
      </form>
    </AuthShell>
  );
}
