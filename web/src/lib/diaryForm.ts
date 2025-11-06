import type { DiaryEntry, UpsertDiaryRequest } from "@/lib/api/types";
import { blankToNull, type DiaryForm } from "@/lib/schemas";

export const STATUS_LABELS = {
  WantToPlay: "Want to play",
  Playing: "Playing",
  Completed: "Completed",
  Abandoned: "Abandoned",
  OnHold: "On hold",
} as const;

export const DEFAULT_DIARY: DiaryForm = {
  status: "Playing",
  platformId: null,
  startedOn: "",
  completedOn: "",
  hours: 0,
  minutes: 0,
  recommended: null,
  notes: "",
};

export const totalMinutes = (hours: number, minutes: number) => Math.max(0, Math.trunc(hours)) * 60 + Math.max(0, Math.trunc(minutes));

export function fromEntry(entry: DiaryEntry): DiaryForm {
  const latest = entry.playthroughs.at(-1);
  const minutes = latest?.playtimeMinutes ?? 0;
  return {
    status: entry.status,
    platformId: entry.platformId,
    startedOn: entry.startedOn ?? "",
    completedOn: entry.completedOn ?? "",
    hours: Math.floor(minutes / 60),
    minutes: minutes % 60,
    recommended: entry.recommended,
    notes: entry.notes ?? "",
  };
}

/**
 * Form → API. A completion date only makes sense for completed games, so it is dropped for other statuses
 * rather than silently sent and rejected.
 */
export function toRequest(values: DiaryForm): UpsertDiaryRequest {
  return {
    status: values.status,
    platformId: values.platformId,
    startedOn: blankToNull(values.startedOn),
    completedOn: values.status === "Completed" ? blankToNull(values.completedOn) : null,
    playtimeMinutes: totalMinutes(values.hours, values.minutes),
    recommended: values.recommended,
    notes: blankToNull(values.notes),
  };
}
