"use client";

import Link from "next/link";
import { useState } from "react";
import { useCart } from "@/components/CartProvider";
import { formatPrice } from "@/lib/format";

export default function CartPage() {
  const { cart, setItem, removeItem } = useCart();
  const [error, setError] = useState<string | null>(null);

  async function run(action: () => Promise<void>) {
    setError(null);
    try {
      await action();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Something went wrong.");
    }
  }

  if (!cart || cart.items.length === 0) {
    return (
      <section className="space-y-4">
        <h1 className="text-2xl font-semibold">Your cart</h1>
        <p className="text-stone-600">
          Your cart is empty. <Link href="/" className="underline">Browse books</Link>
        </p>
      </section>
    );
  }

  return (
    <section className="max-w-3xl space-y-6">
      <h1 className="text-2xl font-semibold">Your cart</h1>

      <ul className="divide-y divide-stone-200 rounded-md border border-stone-200 bg-white">
        {cart.items.map((item) => (
          <li key={item.isbn} className="flex flex-wrap items-center gap-4 p-4">
            <div className="flex-1">
              <Link href={`/books/${item.isbn}`} className="font-medium hover:underline">
                {item.title}
              </Link>
              <p className="text-sm text-stone-500">{formatPrice(item.unitPrice)} each</p>
            </div>

            <select
              aria-label={`Quantity of ${item.title}`}
              value={item.quantity}
              onChange={(e) => run(() => setItem(item.isbn, Number(e.target.value)))}
              className="rounded-md border border-stone-300 px-2 py-1"
            >
              {Array.from({ length: 10 }, (_, i) => i + 1).map((n) => (
                <option key={n} value={n}>
                  {n}
                </option>
              ))}
            </select>

            <p className="w-24 text-right font-semibold">{formatPrice(item.lineTotal)}</p>

            <button
              onClick={() => run(() => removeItem(item.isbn))}
              className="text-sm text-stone-500 underline hover:text-rose-700"
            >
              Remove
            </button>
          </li>
        ))}
      </ul>

      {error && (
        <p className="text-sm text-rose-700" role="alert">
          {error}
        </p>
      )}

      <div className="flex items-center justify-between">
        <p className="text-lg">
          Total <span className="font-semibold" data-testid="cart-total">{formatPrice(cart.total)}</span>
        </p>
        <Link
          href="/checkout"
          className="rounded-md bg-rose-700 px-5 py-2 font-medium text-white hover:bg-rose-800"
        >
          Checkout
        </Link>
      </div>
    </section>
  );
}
