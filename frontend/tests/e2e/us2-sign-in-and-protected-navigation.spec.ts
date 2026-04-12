import { test, expect } from '@playwright/test'
import { signIn } from './helpers'

test.describe('User Story 2: Sign In and Protected Navigation', () => {
  test('successful signin flow lands on timeline', async ({ page }) => {
    await signIn(page)
    await expect(page).toHaveURL('/')
    await expect(page.getByRole('heading', { name: 'Home' })).toBeVisible()
  })

  test('invalid credentials show generic error', async ({ page }) => {
    await page.goto('/signin')

    await page.getByTestId('username-input').fill('alice')
    await page.getByTestId('password-input').fill('wrongpassword')
    await page.getByTestId('submit-button').click()

    await expect(page.getByRole('alert')).toContainText('Invalid username or password')
    await expect(page).toHaveURL('/signin')
  })

  test('protected route redirects to signin with return URL', async ({ page }) => {
    // Visit protected route when not authenticated
    await page.goto('/')

    // Should redirect to signin with return URL
    await expect(page).toHaveURL('/signin?returnUrl=%2F')

    // Sign in
    await page.getByTestId('username-input').fill('alice')
    await page.getByTestId('password-input').fill('TestPassword123')
    await page.getByTestId('submit-button').click()

    // Should redirect back to original URL
    await expect(page).toHaveURL('/')
    await expect(page.getByRole('heading', { name: 'Home' })).toBeVisible()
  })

  test('unauthenticated /u/me redirects to signin with return URL', async ({ page }) => {
    await page.goto('/u/me')

    await expect(page).toHaveURL('/signin?returnUrl=%2Fu%2Fme')
  })
})
