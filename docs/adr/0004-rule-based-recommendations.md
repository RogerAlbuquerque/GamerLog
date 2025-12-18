# ADR 0004 — Rule-based, explainable recommendations

**Status:** accepted

## Context

The product wants "games you might like" without a data-science stack. A new community has little data, so
collaborative filtering would be noisy, and users trust suggestions that can explain themselves.

## Decision

Recommendations are computed from the caller's own diary with explicit rules (`Application/Discovery/Recommender.cs`):

- Each diary entry yields a *signal*: status (completed > playing > want to play > on hold; abandoned is negative)
  plus a rating centred on three stars.
- Signals are summed per genre into a taste profile; platforms contribute with a smaller weight.
- A candidate scores the sum of its genre weights, a platform bonus and a small quality bonus from its (Bayesian
  smoothed) community rating. Games already in the diary are never suggested.
- Each suggestion lists the genres that drove it ("Because you like RPG").

The calculation is a pure function over plain records, so it is unit-tested without a database.

## Alternatives considered

Collaborative filtering or embeddings: better at scale, but opaque, data-hungry and operationally heavier.

## Consequences

Cheap, deterministic and explainable. Weights are constants that can be tuned; quality is limited to genre and
platform, so it cannot discover taste across unrelated genres.
