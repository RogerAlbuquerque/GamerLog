# GameLog web

Next.js 15 (App Router) + React 19 + TypeScript + Tailwind CSS 4.

```bash
npm ci
npm run dev        # http://localhost:3000, proxies /api/* to API_INTERNAL_URL (default http://localhost:5080)
npm run lint && npm run typecheck && npm test
npm run build && npm start
```

```
src/app/           routes (thin; each renders a view component)
src/components/    ui primitives and feature components (games, diary, reviews, comments, collections, …)
src/lib/api/       typed HTTP client (CSRF, error mapping) and endpoint functions
src/lib/           schemas (Zod), formatting and pure helpers
src/stores/        Zustand stores: session, toast
src/hooks/         useAsync, useDebounced, useRequireAuth, useUnreadCount
```

User-written text is always rendered as text, never as HTML. See `../docs/architecture.md`.
