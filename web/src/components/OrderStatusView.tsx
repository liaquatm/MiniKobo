"use client";

import Link from "next/link";
import { use, useEffect, useState } from "react";
import { formatPrice } from "@/lib/format";
import { ApiError, ordersApi } from "@/lib/orders-api";
import type { Order, OrderStatus } from "@/lib/types";

const POLL_INTERVAL_MS = 1500;

const steps: { status: OrderStatus; label: string }[] = [
  { status: "Placed", label: "Order placed" },
  { status: "StockReserved", label: "Stock reserved" },
  { status: "Paid", label: "Payment taken" },
  { status: "Confirmed", label: "Confirmed, email sent" },
];

export function OrderStatusView({ params }: { params: Promise<{ orderId: string }> }) {
  const { orderId } = use(params);
  const [order, setOrder] = useState<Order | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Poll until the order reaches a final state. The order is processed asynchronously
  // (messages + saga), so right after checkout it may not exist yet (404): keep polling.
  useEffect(() => {
    let stopped = false;
    let timer: ReturnType<typeof setTimeout>;

    async function poll() {
      try {
        const latest = await ordersApi.getOrder(orderId);
        if (stopped) return;
        setOrder(latest);
        if (latest.status === "Confirmed" || latest.status === "Failed") return;
      } catch (e) {
        if (stopped) return;
        if (!(e instanceof ApiError && e.status === 404)) {
          setError(e instanceof Error ? e.message : "Could not load the order.");
        }
      }
      timer = setTimeout(poll, POLL_INTERVAL_MS);
    }

    poll();
    return () => {
      stopped = true;
      clearTimeout(timer);
    };
  }, [orderId]);

  if (!order) {
    return <p className="text-stone-600">{error ?? "Placing your order…"}</p>;
  }

  const reached = steps.findIndex((step) => step.status === order.status);

  return (
    <section className="max-w-2xl space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">
          {order.status === "Confirmed" ? "Thank you! Your order is confirmed." :
            order.status === "Failed" ? "We couldn't complete your order." : "Processing your order…"}
        </h1>
        <p className="text-sm text-stone-500">Order {order.id}</p>
      </div>

      {order.status === "Failed" ? (
        <p className="rounded-md border border-rose-200 bg-rose-50 p-4 text-rose-800" data-testid="order-status">
          Failed: {order.failureReason}
        </p>
      ) : (
        <ol className="space-y-2" data-testid="order-status" data-status={order.status}>
          {steps.map((step, index) => (
            <li key={step.status} className="flex items-center gap-3">
              <span className={`flex h-6 w-6 items-center justify-center rounded-full text-xs text-white ${
                index <= reached ? "bg-emerald-600" : "bg-stone-300"}`}>
                {index <= reached ? "✓" : index + 1}
              </span>
              <span className={index <= reached ? "font-medium" : "text-stone-500"}>{step.label}</span>
            </li>
          ))}
        </ol>
      )}

      <div className="rounded-md border border-stone-200 bg-white p-4">
        <ul className="space-y-2 text-sm">
          {order.items.map((item) => (
            <li key={item.isbn} className="flex justify-between gap-4">
              <span>{item.quantity} × {item.title}</span>
              <span>{formatPrice(item.lineTotal)}</span>
            </li>
          ))}
        </ul>
        <p className="mt-2 flex justify-between border-t border-stone-200 pt-2 font-semibold">
          <span>Total</span>
          <span>{formatPrice(order.total)}</span>
        </p>
      </div>

      <p className="text-sm text-stone-600">
        A confirmation is sent to {order.customerEmail}.{" "}
        <a href="http://localhost:8025" target="_blank" rel="noreferrer" className="underline">
          Open the Mailpit inbox
        </a>{" "}
        · <Link href="/" className="underline">Continue shopping</Link>
      </p>
    </section>
  );
}
