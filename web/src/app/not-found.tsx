import Link from "next/link";

export default function NotFound() {
  return (
    <section className="space-y-4">
      <h1 className="text-2xl font-semibold">Not found</h1>
      <p className="text-stone-600">
        We couldn&apos;t find that page. <Link href="/" className="underline">Browse books</Link>
      </p>
    </section>
  );
}
