import { test, expect } from '@playwright/test'

test.describe('Startup and Readiness', () => {
  test('backend serves SPA entry point', async ({ page }) => {
    await page.goto('/')
    await expect(page.locator('#root')).toBeVisible()
  })
})
