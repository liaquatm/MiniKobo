"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { useCart } from "@/components/CartProvider";
import { formatPrice } from "@/lib/format";
import { ordersApi } from "@/lib/orders-api";

export default function CheckoutPage() {
  const router = useRouter();
  const { cart, forgetCart } = useCart();
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (!cart || cart.items.length === 0) {
    return (
      <p className="text-stone-600">
        Your cart is empty. <Link href="/" className="underline">Browse books</Link>
      </p>
    );
  }

  async function placeOrder(formData: FormData) {
    if (!cart) {
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      const { orderId } = await ordersApi.placeOrder(
        cart.id,
        String(formData.get("name")),
        String(formData.get("email")),
      );
      forgetCart(); // the server turns the cart into the order
      router.push(`/orders/${orderId}`);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Could not place the order.");
      setSubmitting(false);
    }
  }

  return (
    <section className="grid max-w-4xl gap-10 md:grid-cols-[1fr_320px]">
      <form action={placeOrder} className="space-y-5">
        <h1 className="text-2xl font-semibold">Checkout</h1>

        <label className="block space-y-1">
          <span className="text-sm font-medium">Name</span>
          <input name="name" required maxLength={200} autoComplete="name"
            className="w-full rounded-md border border-stone-300 px-3 py-2" />
        </label>

        <label className="block space-y-1">
          <span className="text-sm font-medium">Email</span>
          <input name="email" type="email" required maxLength={320} autoComplete="email"
            className="w-full rounded-md border border-stone-300 px-3 py-2" />
          <span className="block text-xs text-stone-500">
            Demo tip: an address containing <code>+fail</code> (e.g. you+fail@example.com) has its payment declined.
          </span>
        </label>

        {error && (
          <p className="text-sm text-rose-700" role="alert">
            {error}
          </p>
        )}

        <button type="submit" disabled={submitting}
          className="rounded-md bg-rose-700 px-6 py-2 font-medium text-white hover:bg-rose-800 disabled:opacity-60">
          {submitting ? "Placing order…" : `Place order · ${formatPrice(cart.total)}`}
        </button>
      </form>

      <aside className="h-fit space-y-3 rounded-md border border-stone-200 bg-white p-4">
        <h2 className="font-semibold">Order summary</h2>
        <ul className="space-y-2 text-sm">
          {cart.items.map((item) => (
            <li key={item.isbn} className="flex justify-between gap-4">
              <span>{item.quantity} × {item.title}</span>
              <span>{formatPrice(item.lineTotal)}</span>
            </li>
          ))}
        </ul>
        <p className="flex justify-between border-t border-stone-200 pt-2 font-semibold">
          <span>Total</span>
          <span>{formatPrice(cart.total)}</span>
        </p>
      </aside>
    </section>
  );
}
