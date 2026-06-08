import { expect, test } from '@playwright/test';
import path from 'node:path';

test('upload, list, and download a file', async ({ page }) => {
  const uniqueTag = `e2e-${Date.now()}`;
  const fixturePath = path.join(__dirname, 'fixtures', 'e2e-upload.pdf');

  await page.goto('/login');
  await page.getByLabel('User ID').fill('e2e-user');
  await page.getByLabel('Role').selectOption('user');
  await page.getByRole('button', { name: 'Create token' }).click();

  await expect(page).toHaveURL(/\/storage\/files/);

  await page.getByRole('link', { name: 'Upload file' }).click();
  await page.locator('input[type="file"]').setInputFiles(fixturePath);
  await page.getByLabel('Tags').fill(uniqueTag);
  await page.getByRole('button', { name: 'Upload file' }).click();

  await expect(page).toHaveURL(/\/storage\/files/);
  await page.getByLabel('Tag').fill(uniqueTag);
  await page.getByRole('button', { name: 'Apply filters' }).click();

  await expect(page.getByRole('table')).toContainText('e2e-upload.pdf');
  await expect(page.getByRole('table')).toContainText(uniqueTag);

  const downloadPromise = page.waitForEvent('download');
  await page.getByRole('button', { name: 'Download' }).first().click();
  const download = await downloadPromise;

  expect(download.suggestedFilename()).toBe('e2e-upload.pdf');
});
