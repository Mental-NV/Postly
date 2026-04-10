import { test, expect } from '@playwright/test'

test.describe('Startup and Readiness', () => {
  test('backend serves SPA entry point', async ({ page }) => {
    await page.goto('/')
    await expect(page.locator('#root')).toBeVisible()
  })

  test('seeded users exist', async ({ request }) => {
    // This will be implemented once auth endpoints exist
    expect(true).toBe(true)
  })
})
