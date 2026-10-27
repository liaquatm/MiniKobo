# MiniKobo storefront

The Next.js (App Router, TypeScript, Tailwind CSS) front end for MiniKobo. See the [root README](../README.md) for how to run the whole system.

- `src/app`: pages (catalog, book, cart, checkout, order status)
- `src/components`: UI components, including `CartProvider` (cart state shared across the app)
- `src/lib`: API helpers and TypeScript types mirroring the .NET responses
- `e2e`: Playwright end-to-end tests (`npm run test:e2e`)

Server Components fetch the Catalog API directly. Browser code calls `/api/catalog/*` and `/api/orders/*`, which `next.config.ts` forwards to the .NET APIs.
