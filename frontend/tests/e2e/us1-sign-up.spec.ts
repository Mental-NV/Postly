import { test, expect } from '@playwright/test'

test.describe('User Story 1: Sign Up', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/signup')
  })

  test('successful signup flow lands on timeline', async ({ page }) => {
    const uniqueUsername = `user${Date.now()}`

    await page.getByTestId('username-input').fill(uniqueUsername)
    await page.getByTestId('displayName-input').fill('Test User')
    await page.getByTestId('bio-input').fill('This is my test bio')
    await page.getByTestId('password-input').fill('TestPassword123')

    await page.getByTestId('submit-button').click()

    await expect(page).toHaveURL('/')
    await expect(page.getByRole('heading', { name: 'Home' })).toBeVisible()
  })

  test('duplicate username shows conflict message', async ({ page }) => {
    const uniqueUsername = `user${Date.now()}`

    // First signup
    await page.getByTestId('username-input').fill(uniqueUsername)
    await page.getByTestId('displayName-input').fill('First User')
    await page.getByTestId('password-input').fill('TestPassword123')
    await page.getByTestId('submit-button').click()

    await expect(page).toHaveURL('/')
    await expect(page.getByRole('heading', { name: 'Home' })).toBeVisible()

    // Navigate back to signup
    await page.goto('/signup')

    // Try to signup with same username
    await page.getByTestId('username-input').fill(uniqueUsername)
    await page.getByTestId('displayName-input').fill('Second User')
    await page.getByTestId('password-input').fill('TestPassword456')
    await page.getByTestId('submit-button').click()

    await expect(page.getByTestId('username-error')).toHaveText(
      'Username is already taken.'
    )
  })
})
