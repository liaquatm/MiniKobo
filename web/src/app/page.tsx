import Link from "next/link";
import { Suspense } from "react";
import { BookCover } from "@/components/BookCover";
import { getBooks, PAGE_SIZE } from "@/lib/catalog";
import { formatPrice } from "@/lib/format";

type SearchParams = Promise<{ search?: string; page?: string }>;

export default function CatalogPage({ searchParams }: { searchParams: SearchParams }) {
  // Reading searchParams and fetching books happen at request time, so they stream inside Suspense.
  return (
    <Suspense fallback={<p className="text-stone-500">Loading books…</p>}>
      <BookResults searchParams={searchParams} />
    </Suspense>
  );
}

async function BookResults({ searchParams }: { searchParams: SearchParams }) {
  const { search, page: pageParam } = await searchParams;
  const page = Math.max(1, Number(pageParam) || 1);
  const result = await getBooks(search, page);
  const pageCount = Math.max(1, Math.ceil(result.totalCount / PAGE_SIZE));

  const pageHref = (target: number) =>
    `/?${new URLSearchParams({ ...(search ? { search } : {}), page: String(target) })}`;

  return (
    <section>
      <h1 className="mb-6 text-2xl font-semibold">
        {search ? `Results for “${search}”` : "All books"}
        <span className="ml-2 text-base font-normal text-stone-500">({result.totalCount})</span>
      </h1>

      {result.items.length === 0 ? (
        <p className="text-stone-600">
          No books found. <Link href="/" className="underline">Browse all books</Link>
        </p>
      ) : (
        <ul className="grid grid-cols-2 gap-6 sm:grid-cols-3 lg:grid-cols-5">
          {result.items.map((book) => (
            <li key={book.isbn}>
              <Link href={`/books/${book.isbn}`} className="group block space-y-2">
                <BookCover isbn={book.isbn} title={book.title} />
                <div>
                  <p className="font-medium leading-snug group-hover:underline">{book.title}</p>
                  <p className="text-sm text-stone-600">{book.author}</p>
                  <p className="text-sm font-semibold">
                    {formatPrice(book.price)}
                    {book.stockQuantity === 0 && (
                      <span className="ml-2 font-normal text-stone-500">Out of stock</span>
                    )}
                  </p>
                </div>
              </Link>
            </li>
          ))}
        </ul>
      )}

      {pageCount > 1 && (
        <nav className="mt-8 flex items-center gap-4 text-sm">
          {page > 1 && <Link href={pageHref(page - 1)} className="underline">Previous</Link>}
          <span>
            Page {page} of {pageCount}
          </span>
          {page < pageCount && <Link href={pageHref(page + 1)} className="underline">Next</Link>}
        </nav>
      )}
    </section>
  );
}
