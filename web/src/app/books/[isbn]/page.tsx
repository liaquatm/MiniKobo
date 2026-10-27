import Link from "next/link";
import { notFound } from "next/navigation";
import { Suspense } from "react";
import { AddToCart } from "@/components/AddToCart";
import { BookCover } from "@/components/BookCover";
import { getBook } from "@/lib/catalog";
import { formatPrice } from "@/lib/format";

type Params = Promise<{ isbn: string }>;

export default function BookPage({ params }: { params: Params }) {
  return (
    <Suspense fallback={<p className="text-stone-500">Loading book…</p>}>
      <BookDetails params={params} />
    </Suspense>
  );
}

async function BookDetails({ params }: { params: Params }) {
  const { isbn } = await params;
  const book = await getBook(isbn);
  if (!book) {
    notFound();
  }

  return (
    <article className="grid gap-8 sm:grid-cols-[240px_1fr]">
      <BookCover isbn={book.isbn} title={book.title} large />

      <div className="space-y-4">
        <Link href="/" className="text-sm text-stone-500 underline">
          ← Back to books
        </Link>
        <h1 className="text-3xl font-semibold">{book.title}</h1>
        <p className="text-lg text-stone-600">{book.author}</p>
        <p className="text-2xl font-semibold">{formatPrice(book.price)}</p>
        <p className="max-w-prose leading-relaxed text-stone-700">{book.description}</p>
        <p className="text-sm text-stone-500">
          ISBN {book.isbn} · {book.stockQuantity > 0 ? `${book.stockQuantity} in stock` : "Out of stock"}
        </p>
        <AddToCart isbn={book.isbn} stockQuantity={book.stockQuantity} />
      </div>
    </article>
  );
}
