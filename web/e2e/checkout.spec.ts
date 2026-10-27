import { expect, test, type Page } from "@playwright/test";

// Each test gets a fresh browser context, so a fresh (empty) cart.

async function addBookToCart(page: Page, search: string, title: string) {
  await page.goto("/");
  await page.getByRole("searchbox", { name: "Search books" }).fill(search);
  await page.getByRole("searchbox", { name: "Search books" }).press("Enter");
  await expect(page.getByRole("heading", { name: `Results for “${search}”` })).toBeVisible();

  await page.getByRole("link", { name: title }).first().click();
  await page.getByRole("button", { name: "Add to cart" }).click();
  await expect(page.getByRole("status")).toContainText("Added");
}

async function checkOut(page: Page, email: string) {
  await page.getByRole("link", { name: /^Cart/ }).click();
  await page.getByRole("link", { name: "Checkout" }).click();
  await page.getByLabel("Name").fill("Playwright Reader");
  await page.getByLabel("Email").fill(email);
  await page.getByRole("button", { name: /Place order/ }).click();
  await expect(page).toHaveURL(/\/orders\//);
}

test("a customer finds a book, checks out and sees the order confirmed", async ({ page }) => {
  await addBookToCart(page, "frankenstein", "Frankenstein");
  await checkOut(page, "e2e@example.com");

  // The order is processed asynchronously (messages + saga); the page polls until it's done.
  await expect(page.getByRole("heading", { name: "Thank you! Your order is confirmed." }))
    .toBeVisible({ timeout: 30_000 });
});

test("a declined payment fails the order and explains why", async ({ page }) => {
  await addBookToCart(page, "dracula", "Dracula");
  await checkOut(page, "e2e+fail@example.com");

  await expect(page.getByTestId("order-status")).toContainText("Payment was declined", { timeout: 30_000 });
});
