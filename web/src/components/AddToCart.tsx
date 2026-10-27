"use client";

import Link from "next/link";
import { useState } from "react";
import { useCart } from "./CartProvider";

export function AddToCart({ isbn, stockQuantity }: { isbn: string; stockQuantity: number }) {
  const { cart, setItem } = useCart();
  const [quantity, setQuantity] = useState(1);
  const [status, setStatus] = useState<"idle" | "saving" | "added">("idle");
  const [error, setError] = useState<string | null>(null);

  if (stockQuantity === 0) {
    return <p className="font-medium text-stone-500">Out of stock</p>;
  }

  const inCart = cart?.items.find((item) => item.isbn === isbn)?.quantity ?? 0;
  const maxQuantity = Math.min(10, stockQuantity);

  async function add() {
    setStatus("saving");
    setError(null);
    try {
      await setItem(isbn, Math.min(inCart + quantity, maxQuantity));
      setStatus("added");
    } catch (e) {
      setError(e instanceof Error ? e.message : "Could not add to cart.");
      setStatus("idle");
    }
  }

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-3">
        <label className="text-sm text-stone-600">
          Quantity{" "}
          <select
            value={quantity}
            onChange={(e) => setQuantity(Number(e.target.value))}
            className="ml-1 rounded-md border border-stone-300 px-2 py-1"
          >
            {Array.from({ length: maxQuantity }, (_, i) => i + 1).map((n) => (
              <option key={n} value={n}>
                {n}
              </option>
            ))}
          </select>
        </label>
        <button
          onClick={add}
          disabled={status === "saving"}
          className="rounded-md bg-rose-700 px-5 py-2 font-medium text-white hover:bg-rose-800 disabled:opacity-60"
        >
          {status === "saving" ? "Adding…" : "Add to cart"}
        </button>
      </div>

      {status === "added" && (
        <p className="text-sm text-emerald-700" role="status">
          Added. <Link href="/cart" className="underline">View cart</Link>
        </p>
      )}
      {error && (
        <p className="text-sm text-rose-700" role="alert">
          {error}
        </p>
      )}
    </div>
  );
}
