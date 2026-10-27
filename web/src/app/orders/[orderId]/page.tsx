import { Suspense } from "react";
import { OrderStatusView } from "@/components/OrderStatusView";

export default function OrderPage({ params }: { params: Promise<{ orderId: string }> }) {
  // params is request-time data, so the client view that reads it streams inside Suspense.
  return (
    <Suspense fallback={<p className="text-stone-600">Loading your order…</p>}>
      <OrderStatusView params={params} />
    </Suspense>
  );
}
