import { test, expect } from '@playwright/test';

test.describe('Capture sync flow', () => {
  test('stores offline then syncs when online', async ({ page, context }) => {
    test.fixme(true, 'Requires deployed environment with seed data and offline simulator.');

    await page.goto('/');

    await page.fill('input#name', 'Operator');
    await page.fill('input#model', 'Model-X');
    await page.fill('input#partNumber', 'PN-123');
    await page.fill('input#quantity', '2');
    await page.fill('input#price', '5');
    await page.click('button[type=submit]');

    await expect(page.getByText('Pending sync')).toBeVisible();
    await page.click('button:has-text("Sync now")');
    await expect(page.getByText('Synced')).toBeVisible();
  });
});
