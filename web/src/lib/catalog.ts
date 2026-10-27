import "server-only";
import type { Book, PagedResponse } from "./types";

// Server Components call the Catalog API directly (server to server), not through the /api rewrite.
const catalogApiUrl = process.env.CATALOG_API_URL ?? "http://localhost:5100";

export const PAGE_SIZE = 12;

export async function getBooks(search: string | undefined, page: number): Promise<PagedResponse<Book>> {
  const query = new URLSearchParams({ page: String(page), pageSize: String(PAGE_SIZE) });
  if (search) {
    query.set("search", search);
  }

  const response = await fetch(`${catalogApiUrl}/books?${query}`);
  if (!response.ok) {
    throw new Error(`Catalog API returned ${response.status}`);
  }

  return response.json();
}

export async function getBook(isbn: string): Promise<Book | null> {
  const response = await fetch(`${catalogApiUrl}/books/${encodeURIComponent(isbn)}`);
  if (response.status === 404) {
    return null;
  }

  if (!response.ok) {
    throw new Error(`Catalog API returned ${response.status}`);
  }

  return response.json();
}
