import type { Cart, ProblemDetails } from "./types";

// Browser-side calls to the Orders API, through the /api/orders rewrite in next.config.ts.

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    title: string,
    public readonly detail?: string,
  ) {
    super(title);
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/api/orders${path}`, {
    ...init,
    headers: { "Content-Type": "application/json", ...init?.headers },
  });

  if (!response.ok) {
    const problem: ProblemDetails | null = await response.json().catch(() => null);
    throw new ApiError(response.status, problem?.title ?? "Something went wrong.", problem?.detail);
  }

  return (response.status === 204 ? undefined : await response.json()) as T;
}

export const ordersApi = {
  createCart: () => request<Cart>("/carts", { method: "POST" }),

  getCart: (cartId: string) => request<Cart>(`/carts/${cartId}`),

  setItem: (cartId: string, isbn: string, quantity: number) =>
    request<Cart>(`/carts/${cartId}/items/${encodeURIComponent(isbn)}`, {
      method: "PUT",
      body: JSON.stringify({ quantity }),
    }),

  removeItem: (cartId: string, isbn: string) =>
    request<void>(`/carts/${cartId}/items/${encodeURIComponent(isbn)}`, { method: "DELETE" }),
};
