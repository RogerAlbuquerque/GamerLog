"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { Avatar } from "@/components/ui/Avatar";
import { Button } from "@/components/ui/Button";
import { Checkbox, Input, Textarea } from "@/components/ui/Field";
import { errorMessage } from "@/components/ui/States";
import { usersApi } from "@/lib/api/endpoints";
import type { Profile, SessionUser } from "@/lib/api/types";
import { applyServerError } from "@/lib/forms";
import { profileSchema, type ProfileForm } from "@/lib/schemas";
import { useSession } from "@/stores/session";
import { toast } from "@/stores/toast";

const MAX_AVATAR_BYTES = 5 * 1024 * 1024;

export function ProfileSettings({ user, profile }: { user: SessionUser; profile: Profile }) {
  const setUser = useSession((s) => s.setUser);
  const fileRef = useRef<HTMLInputElement>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [avatarBusy, setAvatarBusy] = useState(false);
  const {
    register,
    handleSubmit,
    setError,
    reset,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<ProfileForm>({
    resolver: zodResolver(profileSchema),
    defaultValues: { displayName: user.displayName, bio: profile.bio ?? "", showSpoilersByDefault: user.showSpoilersByDefault },
  });

  const onSubmit = handleSubmit(async (v) => {
    setFormError(null);
    try {
      const updated = await usersApi.update(v);
      setUser(updated);
      reset({ displayName: updated.displayName, bio: v.bio, showSpoilersByDefault: updated.showSpoilersByDefault });
      toast.success("Profile saved.");
    } catch (error) {
      setFormError(applyServerError(error, setError, ["displayName", "bio"]));
    }
  });

  const uploadAvatar = async (file: File | undefined) => {
    if (!file) return;
    if (!["image/png", "image/jpeg", "image/webp"].includes(file.type)) return toast.error("Choose a PNG, JPEG or WebP image.");
    if (file.size > MAX_AVATAR_BYTES) return toast.error("That image is larger than 5 MB.");
    setAvatarBusy(true);
    try {
      const form = new FormData();
      form.append("file", file);
      const { avatarUrl } = await usersApi.setAvatar(form);
      setUser({ ...user, avatarUrl });
      toast.success("Avatar updated.");
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setAvatarBusy(false);
    }
  };

  const removeAvatar = async () => {
    setAvatarBusy(true);
    try {
      await usersApi.removeAvatar();
      setUser({ ...user, avatarUrl: null });
      toast.success("Avatar removed.");
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setAvatarBusy(false);
    }
  };

  return (
    <section className="surface space-y-6 p-6" aria-labelledby="profile-heading">
      <h2 id="profile-heading" className="text-xl font-bold">Profile</h2>
      <div className="flex items-center gap-4">
        <Avatar name={user.displayName} src={user.avatarUrl} size="lg" />
        <div className="space-y-2">
          <div className="flex flex-wrap gap-2">
            <Button variant="secondary" size="sm" loading={avatarBusy} onClick={() => fileRef.current?.click()}>Upload new</Button>
            {user.avatarUrl && <Button variant="ghost" size="sm" disabled={avatarBusy} onClick={removeAvatar}>Remove</Button>}
          </div>
          <p className="text-xs text-muted">PNG, JPEG or WebP up to 5 MB. Images are re-encoded and stripped of metadata.</p>
          <input ref={fileRef} type="file" accept="image/png,image/jpeg,image/webp" className="sr-only" aria-label="Avatar image" onChange={(e) => { void uploadAvatar(e.target.files?.[0]); e.target.value = ""; }} />
        </div>
      </div>
      <form onSubmit={onSubmit} noValidate className="space-y-4">
        {formError && <p role="alert" className="rounded-xl border border-danger/40 bg-danger/10 px-4 py-3 text-sm text-danger">{formError}</p>}
        <Input label="Display name" maxLength={50} error={errors.displayName?.message} {...register("displayName")} />
        <Textarea label="Bio" rows={4} maxLength={500} hint="Shown on your profile." error={errors.bio?.message} {...register("bio")} />
        <Checkbox label="Show spoilers by default" hint="When off, spoiler reviews and comments stay hidden until you click." {...register("showSpoilersByDefault")} />
        <Button type="submit" loading={isSubmitting} disabled={!isDirty}>Save profile</Button>
      </form>
    </section>
  );
}
