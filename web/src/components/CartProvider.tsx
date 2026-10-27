"use client";

import { createContext, useCallback, useContext, useEffect, useState } from "react";
import { ApiError, ordersApi } from "@/lib/orders-api";
import type { Cart } from "@/lib/types";

const CART_ID_KEY = "minikobo.cartId";

type CartContextValue = {
  cart: Cart | null;
  itemCount: number;
  setItem: (isbn: string, quantity: number) => Promise<void>;
  removeItem: (isbn: string) => Promise<void>;
  forgetCart: () => void;
};

const CartContext = createContext<CartContextValue | null>(null);

function readCartId(): string | null {
  try {
    return localStorage.getItem(CART_ID_KEY);
  } catch {
    return null;
  }
}

function writeCartId(cartId: string | null) {
  try {
    if (cartId) {
      localStorage.setItem(CART_ID_KEY, cartId);
    } else {
      localStorage.removeItem(CART_ID_KEY);
    }
  } catch {
    // Storage unavailable (e.g. private mode): the cart just won't survive a reload.
  }
}

export function CartProvider({ children }: { children: React.ReactNode }) {
  const [cart, setCart] = useState<Cart | null>(null);

  // On first load, restore the cart whose id the browser remembered.
  useEffect(() => {
    const cartId = readCartId();
    if (!cartId) {
      return;
    }

    ordersApi
      .getCart(cartId)
      .then(setCart)
      .catch((error) => {
        if (error instanceof ApiError && error.status === 404) {
          writeCartId(null); // The cart no longer exists on the server; start fresh next time.
        }
      });
  }, []);

  const ensureCartId = useCallback(async (): Promise<string> => {
    const existing = cart?.id ?? readCartId();
    if (existing) {
      return existing;
    }

    const created = await ordersApi.createCart();
    writeCartId(created.id);
    setCart(created);
    return created.id;
  }, [cart]);

  const setItem = useCallback(
    async (isbn: string, quantity: number) => {
      const cartId = await ensureCartId();
      try {
        setCart(await ordersApi.setItem(cartId, isbn, quantity));
      } catch (error) {
        // A remembered cart can disappear (e.g. database reset): create a new one and retry once.
        if (error instanceof ApiError && error.status === 404 && error.message === "Cart not found.") {
          const created = await ordersApi.createCart();
          writeCartId(created.id);
          setCart(await ordersApi.setItem(created.id, isbn, quantity));
          return;
        }
        throw error;
      }
    },
    [ensureCartId],
  );

  const removeItem = useCallback(
    async (isbn: string) => {
      if (!cart) {
        return;
      }
      await ordersApi.removeItem(cart.id, isbn);
      setCart(await ordersApi.getCart(cart.id));
    },
    [cart],
  );

  const forgetCart = useCallback(() => {
    writeCartId(null);
    setCart(null);
  }, []);

  const itemCount = cart?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0;

  return (
    <CartContext.Provider value={{ cart, itemCount, setItem, removeItem, forgetCart }}>
      {children}
    </CartContext.Provider>
  );
}

export function useCart(): CartContextValue {
  const context = useContext(CartContext);
  if (!context) {
    throw new Error("useCart must be used inside <CartProvider>.");
  }
  return context;
}
