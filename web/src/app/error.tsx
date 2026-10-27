"use client";

// Shown when a page throws, e.g. the Catalog API is not running.
export default function Error({ reset }: { error: Error; reset: () => void }) {
  return (
    <section className="space-y-4">
      <h1 className="text-2xl font-semibold">Something went wrong</h1>
      <p className="text-stone-600">We couldn&apos;t load this page. The store&apos;s services may be unavailable.</p>
      <button onClick={reset} className="rounded-md bg-stone-900 px-4 py-2 text-white">
        Try again
      </button>
    </section>
  );
}
