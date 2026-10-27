import Form from "next/form";
import Link from "next/link";
import { CartLink } from "./CartLink";

export function Header() {
  return (
    <header className="border-b border-stone-200 bg-white">
      <div className="mx-auto flex max-w-6xl flex-wrap items-center gap-4 px-4 py-3">
        <Link href="/" className="text-xl font-bold tracking-tight text-stone-900">
          Mini<span className="text-rose-700">Kobo</span>
        </Link>

        {/* next/form submits as a client-side navigation to /?search=... */}
        <Form action="/" className="order-last w-full sm:order-none sm:flex-1">
          <input
            name="search"
            type="search"
            placeholder="Search by title, author or description"
            aria-label="Search books"
            className="w-full rounded-md border border-stone-300 px-3 py-2 text-sm outline-none focus:border-rose-700"
          />
        </Form>

        <CartLink />
      </div>
    </header>
  );
}
