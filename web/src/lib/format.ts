const currency = new Intl.NumberFormat("en-CA", { style: "currency", currency: "CAD" });

export function formatPrice(amount: number): string {
  return currency.format(amount);
}
