import type { Metadata } from "next";
import { PopularStrip } from "@/components/home/PopularStrip";
import { LinkButton } from "@/components/ui/Button";

export const metadata: Metadata = { title: { absolute: "GameLog — your gaming journal" }, description: "Log every game you play, rate and review it, build collections and look back on your year." };

const features = [
  { icon: "📖", title: "A diary for every playthrough", text: "Track status, platform, dates and hours. Replay a game? Log a new run." },
  { icon: "★", title: "Half-star ratings & honest reviews", text: "Rate to the half star and write reviews with spoiler protection built in." },
  { icon: "🗂️", title: "Collections", text: "Curate ordered lists — public to share, private to plan." },
  { icon: "📈", title: "Your stats, your year", text: "Playtime, genres and a shareable year-in-games card every December." },
  { icon: "🤝", title: "Follow friends", text: "A feed of what they finish, rate and review, plus replies and likes." },
  { icon: "✨", title: "Recommendations that explain themselves", text: "Suggestions from your own taste — and they tell you why." },
];

export default function HomePage() {
  return (
    <div className="space-y-20">
      <section className="relative isolate overflow-hidden rounded-3xl border border-line px-6 py-16 text-center sm:py-24">
        <div aria-hidden className="absolute inset-0 -z-10 bg-[radial-gradient(60%_60%_at_50%_0%,color-mix(in_oklab,var(--color-accent)_35%,transparent),transparent),radial-gradient(40%_40%_at_90%_100%,color-mix(in_oklab,var(--color-mint)_18%,transparent),transparent)]" />
        <p className="text-sm font-semibold uppercase tracking-[0.3em] text-accent-soft">Press start on your backlog</p>
        <h1 className="mx-auto mt-4 max-w-3xl font-display text-5xl font-extrabold leading-[1.05] sm:text-7xl">
          Every game you play, <span className="bg-gradient-to-r from-accent-soft to-mint bg-clip-text text-transparent">remembered.</span>
        </h1>
        <p className="mx-auto mt-6 max-w-2xl text-lg text-muted">GameLog is the social journal for gamers: log playthroughs, rate and review, build collections and see your year in games.</p>
        <div className="mt-9 flex flex-wrap justify-center gap-3">
          <LinkButton href="/register" size="lg">Start your diary — it&apos;s free</LinkButton>
          <LinkButton href="/discover" size="lg" variant="secondary">Explore games</LinkButton>
        </div>
      </section>

      <PopularStrip />

      <section aria-label="Features" className="space-y-8">
        <h2 className="text-center text-3xl font-bold">Everything a gaming journal should do</h2>
        <ul className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {features.map((f) => (
            <li key={f.title} className="surface space-y-2 p-6">
              <span aria-hidden className="grid size-11 place-items-center rounded-xl bg-ink-700 text-xl">{f.icon}</span>
              <h3 className="text-lg font-semibold">{f.title}</h3>
              <p className="text-sm text-muted">{f.text}</p>
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
}
