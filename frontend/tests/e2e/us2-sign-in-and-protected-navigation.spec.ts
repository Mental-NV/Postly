import { test, expect } from '@playwright/test'

test.describe('User Story 2: Sign In and Protected Navigation', () => {
  test('successful signin flow lands on timeline', async ({ page }) => {
    await page.goto('/signin')

    await page.getByTestId('username-input').fill('alice')
    await page.getByTestId('password-input').fill('TestPassword123')
    await page.getByTestId('submit-button').click()

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

  test('username preserved and password cleared after error', async ({ page }) => {
    await page.goto('/signin')

    await page.getByTestId('username-input').fill('alice')
    await page.getByTestId('password-input').fill('wrongpassword')
    await page.getByTestId('submit-button').click()

    await expect(page.getByRole('alert')).toBeVisible()

    const usernameValue = await page.getByTestId('username-input').inputValue()
    const passwordValue = await page.getByTestId('password-input').inputValue()

    expect(usernameValue).toBe('alice')
    expect(passwordValue).toBe('')
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

  test('signout re-protects routes', async ({ page, context }) => {
    // Sign in first
    await page.goto('/signin')
    await page.getByTestId('username-input').fill('alice')
    await page.getByTestId('password-input').fill('TestPassword123')
    await page.getByTestId('submit-button').click()
    await expect(page).toHaveURL('/')

    // Clear cookies to simulate signout
    await context.clearCookies()

    // Try to visit protected route
    await page.goto('/')

    // Should redirect to signin
    await expect(page).toHaveURL(/\/signin/)
  })

  test('navigation to signup page works', async ({ page }) => {
    await page.goto('/signin')

    await page.getByRole('link', { name: /sign up/i }).click()

    await expect(page).toHaveURL('/signup')
  })

  test('pending state prevents duplicate submission', async ({ page }) => {
    await page.goto('/signin')

    await page.getByTestId('username-input').fill('alice')
    await page.getByTestId('password-input').fill('TestPassword123')

    // Click submit
    await page.getByTestId('submit-button').click()

    // Button should be disabled
    await expect(page.getByTestId('submit-button')).toBeDisabled()
    await expect(page.getByTestId('username-input')).toBeDisabled()
  })

  test('profile navigation lands on the public self profile URL', async ({ page }) => {
    // Sign in as bob
    await page.goto('/signin')
    await page.getByTestId('username-input').fill('bob')
    await page.getByTestId('password-input').fill('TestPassword123')
    await page.getByTestId('submit-button').click()
    await expect(page).toHaveURL('/')

    // Click the Profile nav link
    await page.getByRole('link', { name: 'Profile' }).click()

    // Assert URL becomes the public profile URL
    await expect(page).toHaveURL('/u/bob')

    // Assert the self profile loads
    await expect(page.getByTestId('profile-display-name')).toBeVisible()

    // Assert Edit Profile is visible
    await expect(page.getByTestId('edit-profile-button')).toBeVisible()

    // Assert follow/unfollow buttons are absent
    await expect(page.getByTestId('follow-unfollow-button')).not.toBeVisible()
  })

  test('unauthenticated /u/me redirects to signin with return URL', async ({ page }) => {
    await page.goto('/u/me')

    await expect(page).toHaveURL('/signin?returnUrl=%2Fu%2Fme')
  })

  test('logo sends authenticated users to home', async ({ page }) => {
    await page.goto('/signin')
    await page.getByTestId('username-input').fill('bob')
    await page.getByTestId('password-input').fill('TestPassword123')
    await page.getByTestId('submit-button').click()
    await expect(page).toHaveURL('/')

    await page.goto('/u/bob')
    await expect(page.getByTestId('profile-page')).toBeVisible()

    await page.getByTestId('brand-link').click()
    await expect(page).toHaveURL('/')
  })
})
