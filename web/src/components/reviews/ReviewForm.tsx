"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { Button } from "@/components/ui/Button";
import { Checkbox, Input, Select, Textarea } from "@/components/ui/Field";
import { Modal } from "@/components/ui/Modal";
import { StarInput } from "@/components/ui/StarRating";
import { ApiError } from "@/lib/api/client";
import { reviewsApi } from "@/lib/api/endpoints";
import type { NamedItem, Review } from "@/lib/api/types";
import { applyServerError } from "@/lib/forms";
import { blankToNull, reviewSchema, type ReviewForm as Values } from "@/lib/schemas";
import { toast } from "@/stores/toast";

const EMPTY: Values = {
  title: "", body: "", containsSpoilers: false, halfStars: null, experience: "", pros: "", cons: "",
  favoriteCharacter: "", favoriteMoment: "", difficulty: null, platformId: null,
};

const FIELDS = ["title", "body", "halfStars", "experience", "pros", "cons", "favoriteCharacter", "favoriteMoment"] as const;

const fromReview = (r: Review): Values => ({
  title: r.title, body: r.body, containsSpoilers: r.containsSpoilers, halfStars: r.ratingHalfStars,
  experience: r.experience ?? "", pros: r.pros ?? "", cons: r.cons ?? "", favoriteCharacter: r.favoriteCharacter ?? "",
  favoriteMoment: r.favoriteMoment ?? "", difficulty: r.difficulty, platformId: r.platformId,
});

interface Props {
  open: boolean;
  gameId: string;
  gameTitle: string;
  platforms: NamedItem[];
  onClose: () => void;
  onSaved: (review: Review) => void;
}

/** Write or edit your review. Publishing may also set your star rating in the same request. */
export function ReviewForm({ open, gameId, gameTitle, platforms, onClose, onSaved }: Props) {
  const [existing, setExisting] = useState<Review | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const {
    register,
    control,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<Values>({ resolver: zodResolver(reviewSchema), defaultValues: EMPTY });

  useEffect(() => {
    if (!open) return;
    let cancelled = false;
    setFormError(null);
    reviewsApi
      .mine(gameId)
      .then((r) => {
        if (cancelled) return;
        setExisting(r);
        reset(fromReview(r));
      })
      .catch((e) => {
        if (cancelled) return;
        setExisting(null);
        reset(EMPTY);
        if (!(e instanceof ApiError && e.isNotFound)) setFormError("Could not load your previous review.");
      });
    return () => {
      cancelled = true;
    };
  }, [open, gameId, reset]);

  const onSubmit = handleSubmit(async (v) => {
    setFormError(null);
    try {
      const saved = await reviewsApi.save(gameId, {
        title: v.title,
        body: v.body,
        containsSpoilers: v.containsSpoilers,
        halfStars: v.halfStars,
        experience: blankToNull(v.experience),
        pros: blankToNull(v.pros),
        cons: blankToNull(v.cons),
        favoriteCharacter: blankToNull(v.favoriteCharacter),
        favoriteMoment: blankToNull(v.favoriteMoment),
        difficulty: v.difficulty,
        playtimeMinutes: existing?.playtimeMinutes ?? null,
        platformId: v.platformId,
        completedOn: existing?.completedOn ?? null,
      });
      toast.success(existing ? "Review updated." : "Review published.");
      onSaved(saved);
      onClose();
    } catch (error) {
      setFormError(applyServerError(error, setError, FIELDS));
    }
  });

  return (
    <Modal open={open} onClose={onClose} title={existing ? `Edit your review of ${gameTitle}` : `Review ${gameTitle}`} className="w-[min(94vw,44rem)]">
      <form onSubmit={onSubmit} noValidate className="space-y-4">
        {formError && (
          <p role="alert" className="rounded-xl border border-danger/40 bg-danger/10 px-4 py-3 text-sm text-danger">
            {formError}
          </p>
        )}
        <div>
          <Controller control={control} name="halfStars" render={({ field }) => <StarInput value={field.value} onChange={field.onChange} />} />
          {errors.halfStars?.message && <p role="alert" className="text-xs text-danger">{errors.halfStars.message}</p>}
        </div>
        <Input label="Title" maxLength={120} error={errors.title?.message} {...register("title")} />
        <Textarea label="Your review" rows={7} maxLength={10000} error={errors.body?.message} {...register("body")} />
        <Checkbox label="This review contains spoilers" hint="Readers will have to click to reveal it." {...register("containsSpoilers")} />

        <details className="rounded-xl border border-line p-4">
          <summary className="cursor-pointer text-sm font-semibold">More details (optional)</summary>
          <div className="mt-4 grid gap-4 sm:grid-cols-2">
            <Select label="Difficulty" {...register("difficulty", { setValueAs: (v) => (v ? v : null) })}>
              <option value="">Not set</option>
              <option value="VeryEasy">Very easy</option>
              <option value="Easy">Easy</option>
              <option value="Medium">Medium</option>
              <option value="Hard">Hard</option>
              <option value="VeryHard">Very hard</option>
            </Select>
            <Select label="Played on" {...register("platformId", { setValueAs: (v) => (v === "" || v == null ? null : Number(v)) })}>
              <option value="">Not specified</option>
              {platforms.map((p) => (
                <option key={p.id} value={p.id}>{p.name}</option>
              ))}
            </Select>
            <Input label="Favourite character" maxLength={120} error={errors.favoriteCharacter?.message} {...register("favoriteCharacter")} />
            <Input label="Favourite moment" maxLength={500} error={errors.favoriteMoment?.message} {...register("favoriteMoment")} />
            <Textarea label="Highlights" rows={3} maxLength={1000} error={errors.pros?.message} wrapperClassName="sm:col-span-1" {...register("pros")} />
            <Textarea label="Letdowns" rows={3} maxLength={1000} error={errors.cons?.message} {...register("cons")} />
            <Textarea label="The experience" rows={3} maxLength={3000} error={errors.experience?.message} wrapperClassName="sm:col-span-2" {...register("experience")} />
          </div>
        </details>

        <div className="flex justify-end gap-3 pt-2">
          <Button variant="ghost" onClick={onClose}>Cancel</Button>
          <Button type="submit" loading={isSubmitting}>{existing ? "Save review" : "Publish review"}</Button>
        </div>
      </form>
    </Modal>
  );
}
