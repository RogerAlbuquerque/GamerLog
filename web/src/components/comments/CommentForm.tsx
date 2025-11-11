"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { usePathname, useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/Button";
import { Checkbox, Textarea } from "@/components/ui/Field";
import { errorMessage } from "@/components/ui/States";
import { commentSchema, type CommentForm as Values } from "@/lib/schemas";
import { useSession } from "@/stores/session";
import { toast } from "@/stores/toast";

interface Props {
  onSubmit: (values: Values) => Promise<void>;
  initial?: Values;
  submitLabel?: string;
  placeholder?: string;
  onCancel?: () => void;
  autoFocus?: boolean;
}

/** Compose / edit / reply box. Visitors who are not signed in get a prompt instead of a form. */
export function CommentForm({ onSubmit, initial = { body: "", isSpoiler: false }, submitLabel = "Post comment", placeholder = "Add to the discussion…", onCancel, autoFocus }: Props) {
  const user = useSession((s) => s.user);
  const router = useRouter();
  const pathname = usePathname();
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<Values>({ resolver: zodResolver(commentSchema), defaultValues: initial });

  if (!user) {
    return (
      <p className="surface px-4 py-3 text-sm text-muted">
        <button type="button" className="font-semibold text-accent-soft hover:underline" onClick={() => router.push(`/login?next=${encodeURIComponent(pathname)}`)}>
          Sign in
        </button>{" "}
        to join the conversation.
      </p>
    );
  }

  const submit = handleSubmit(async (values) => {
    try {
      await onSubmit(values);
      reset({ body: "", isSpoiler: false });
    } catch (error) {
      toast.error(errorMessage(error));
    }
  });

  return (
    <form onSubmit={submit} noValidate className="space-y-3">
      <Textarea label="Comment" rows={3} maxLength={2000} placeholder={placeholder} autoFocus={autoFocus} error={errors.body?.message} {...register("body")} />
      <div className="flex flex-wrap items-center justify-between gap-3">
        <Checkbox label="Contains spoilers" {...register("isSpoiler")} />
        <div className="flex gap-2">
          {onCancel && (
            <Button variant="ghost" size="sm" onClick={onCancel}>
              Cancel
            </Button>
          )}
          <Button type="submit" size="sm" loading={isSubmitting}>
            {submitLabel}
          </Button>
        </div>
      </div>
    </form>
  );
}
