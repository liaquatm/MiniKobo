"use client";

import Link from "next/link";
import { useCart } from "./CartProvider";

export function CartLink() {
  const { itemCount } = useCart();

  return (
    <Link
      href="/cart"
      className="ml-auto rounded-md bg-stone-900 px-4 py-2 text-sm font-medium text-white hover:bg-stone-700 sm:ml-0"
    >
      Cart{itemCount > 0 && <span data-testid="cart-count"> ({itemCount})</span>}
    </Link>
  );
}
