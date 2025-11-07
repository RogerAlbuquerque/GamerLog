"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/Button";
import { Input, Select, Textarea } from "@/components/ui/Field";
import { errorMessage } from "@/components/ui/States";
import { ApiError } from "@/lib/api/client";
import { diaryApi } from "@/lib/api/endpoints";
import type { DiaryEntry, NamedItem } from "@/lib/api/types";
import { DEFAULT_DIARY, fromEntry, STATUS_LABELS, toRequest } from "@/lib/diaryForm";
import { applyServerError } from "@/lib/forms";
import { diarySchema, type DiaryForm } from "@/lib/schemas";
import { formatDate, formatPlaytime } from "@/lib/format";
import { toast } from "@/stores/toast";

const FIELDS = ["status", "platformId", "startedOn", "completedOn", "notes"] as const;

/** Add a game to the diary or edit its entry: status, platform, dates, playtime, notes and playthroughs. */
export function DiaryPanel({ gameId, platforms, onChanged }: { gameId: string; platforms: NamedItem[]; onChanged?: (entry: DiaryEntry | null) => void }) {
  const [entry, setEntry] = useState<DiaryEntry | null>(null);
  const [loaded, setLoaded] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    reset,
    setError,
    watch,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<DiaryForm>({ resolver: zodResolver(diarySchema), defaultValues: DEFAULT_DIARY });

  useEffect(() => {
    let cancelled = false;
    diaryApi
      .mine(gameId)
      .then((e) => {
        if (cancelled) return;
        setEntry(e);
        reset(fromEntry(e));
      })
      .catch((e) => {
        if (!(e instanceof ApiError && e.isNotFound)) toast.error(errorMessage(e));
      })
      .finally(() => !cancelled && setLoaded(true));
    return () => {
      cancelled = true;
    };
  }, [gameId, reset]);

  const status = watch("status");

  const apply = (next: DiaryEntry | null) => {
    setEntry(next);
    if (next) reset(fromEntry(next));
    else reset(DEFAULT_DIARY);
    onChanged?.(next);
  };

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);
    try {
      const saved = await diaryApi.save(gameId, toRequest(values));
      apply(saved);
      toast.success(entry ? "Diary entry updated." : "Added to your diary.");
    } catch (error) {
      setFormError(applyServerError(error, setError, FIELDS));
    }
  });

  const remove = async () => {
    if (!window.confirm("Remove this game from your diary? Playthroughs and notes for it will be deleted.")) return;
    try {
      await diaryApi.remove(gameId);
      apply(null);
      toast.success("Removed from your diary.");
    } catch (error) {
      toast.error(errorMessage(error));
    }
  };

  const newPlaythrough = async () => {
    try {
      apply(await diaryApi.addPlaythrough(gameId, { startedOn: null, completedOn: null, playtimeMinutes: 0, platformId: null, notes: null }));
      toast.success("New playthrough started.");
    } catch (error) {
      toast.error(errorMessage(error));
    }
  };

  const removePlaythrough = async (id: string) => {
    if (!window.confirm("Delete this playthrough?")) return;
    try {
      apply(await diaryApi.removePlaythrough(id));
    } catch (error) {
      toast.error(errorMessage(error));
    }
  };

  if (!loaded) return <div className="h-40 animate-shimmer rounded-xl bg-ink-700" aria-hidden />;

  return (
    <form onSubmit={onSubmit} noValidate className="space-y-4">
      {formError && (
        <p role="alert" className="rounded-xl border border-danger/40 bg-danger/10 px-4 py-3 text-sm text-danger">
          {formError}
        </p>
      )}
      <div className="grid gap-4 sm:grid-cols-2">
        <Select label="Status" error={errors.status?.message} {...register("status")}>
          {Object.entries(STATUS_LABELS).map(([value, label]) => (
            <option key={value} value={value}>{label}</option>
          ))}
        </Select>
        <Select label="Platform" error={errors.platformId?.message} {...register("platformId", { setValueAs: (v) => (v === "" || v == null ? null : Number(v)) })}>
          <option value="">Not specified</option>
          {platforms.map((p) => (
            <option key={p.id} value={p.id}>{p.name}</option>
          ))}
        </Select>
        <Input label="Started" type="date" error={errors.startedOn?.message} {...register("startedOn")} />
        {status === "Completed" && <Input label="Completed" type="date" error={errors.completedOn?.message} {...register("completedOn")} />}
        <div className="grid grid-cols-2 gap-3 sm:col-span-2 sm:max-w-xs">
          <Input label="Hours" type="number" min={0} inputMode="numeric" error={errors.hours?.message} {...register("hours", { valueAsNumber: true })} />
          <Input label="Minutes" type="number" min={0} max={59} inputMode="numeric" error={errors.minutes?.message} {...register("minutes", { valueAsNumber: true })} />
        </div>
        <Select label="Would you recommend it?" {...register("recommended", { setValueAs: (v) => (v === "yes" ? true : v === "no" ? false : null) })}>
          <option value="">No opinion</option>
          <option value="yes">Yes</option>
          <option value="no">No</option>
        </Select>
      </div>
      <Textarea label="Private notes" rows={3} maxLength={2000} error={errors.notes?.message} {...register("notes")} />

      <div className="flex flex-wrap items-center gap-3">
        <Button type="submit" loading={isSubmitting} disabled={Boolean(entry) && !isDirty}>
          {entry ? "Save changes" : "Add to diary"}
        </Button>
        {entry && (
          <Button variant="ghost" onClick={remove}>
            Remove
          </Button>
        )}
      </div>

      {entry && (
        <section aria-label="Playthroughs" className="space-y-2 border-t border-line pt-4">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-semibold">Playthroughs · {formatPlaytime(entry.totalPlaytimeMinutes)} total</h3>
            <Button variant="secondary" size="sm" onClick={newPlaythrough}>
              + New playthrough
            </Button>
          </div>
          <ul className="space-y-1.5 text-sm">
            {entry.playthroughs.map((p) => (
              <li key={p.id} className="flex items-center justify-between gap-3 rounded-lg bg-ink-900/60 px-3 py-2">
                <span>
                  <strong>#{p.number}</strong>{" "}
                  <span className="text-muted">
                    {p.startedOn ? formatDate(p.startedOn) : "no start date"}
                    {p.completedOn ? ` → ${formatDate(p.completedOn)}` : ""} · {formatPlaytime(p.playtimeMinutes)}
                    {p.platform ? ` · ${p.platform}` : ""}
                  </span>
                </span>
                {entry.playthroughs.length > 1 && (
                  <button type="button" onClick={() => removePlaythrough(p.id)} className="text-xs text-danger hover:underline" aria-label={`Delete playthrough ${p.number}`}>
                    Delete
                  </button>
                )}
              </li>
            ))}
          </ul>
        </section>
      )}
    </form>
  );
}
