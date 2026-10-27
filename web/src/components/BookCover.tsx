// A generated cover (we have no images): a colour picked from the ISBN, with the title on it.
const palette = ["bg-rose-800", "bg-teal-800", "bg-indigo-800", "bg-amber-700", "bg-emerald-800", "bg-sky-800"];

export function BookCover({ isbn, title, large = false }: { isbn: string; title: string; large?: boolean }) {
  const colour = palette[Number(isbn.slice(-3)) % palette.length];

  return (
    <div
      className={`${colour} flex aspect-[2/3] items-center justify-center rounded-md p-3 text-center font-serif text-white shadow-sm ${
        large ? "text-2xl" : "text-sm"
      }`}
    >
      {title}
    </div>
  );
}
