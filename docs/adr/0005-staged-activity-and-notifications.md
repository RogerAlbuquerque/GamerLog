# ADR 0005 — Activity and notifications written in the caller's transaction

**Status:** accepted

## Context

Feed items and notifications must never describe something that did not happen (a rating that failed to save),
and must not be lost if the process stops between two writes. Spamming followers when someone edits a rating
repeatedly is also undesirable.

## Decision

Services that cause an event call `ActivityService.StageAsync` / `NotificationService.StageAsync`. These methods
only **add rows to the EF change tracker**; the calling service saves once, so the business change, its feed item
and its notification commit atomically.

- Feed items by the same actor, type and subject within 24 hours are merged (the existing row is updated).
- Like and follow notifications are de-duplicated; a user is never notified about their own actions.
- Reading the feed drops items whose subject no longer exists or is no longer visible (deleted reviews, private
  collections).

## Alternatives considered

An outbox with a background worker: more resilient to slow consumers but adds infrastructure and eventual
consistency that this product does not need yet.

## Consequences

Simple and consistent. Writes touch a few more rows per request; if volume grows, staging can move behind an
outbox without changing the callers.
