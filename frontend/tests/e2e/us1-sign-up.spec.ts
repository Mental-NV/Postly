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
    await expect(page.getByText('Timeline Page')).toBeVisible()
  })

  test('validation errors displayed for invalid inputs', async ({ page }) => {
    await page.getByTestId('username-input').fill('ab')
    await page.getByTestId('displayName-input').fill('')
    await page.getByTestId('password-input').fill('short')

    await page.getByTestId('submit-button').click()

    await expect(page.getByTestId('username-error')).toBeVisible()
    await expect(page.getByTestId('displayName-error')).toBeVisible()
    await expect(page.getByTestId('password-error')).toBeVisible()
  })

  test('duplicate username shows conflict message', async ({ page }) => {
    const uniqueUsername = `user${Date.now()}`

    // First signup
    await page.getByTestId('username-input').fill(uniqueUsername)
    await page.getByTestId('displayName-input').fill('First User')
    await page.getByTestId('password-input').fill('TestPassword123')
    await page.getByTestId('submit-button').click()

    await expect(page).toHaveURL('/')

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

  test('pending state prevents duplicate submission', async ({ page }) => {
    const uniqueUsername = `user${Date.now()}`

    await page.getByTestId('username-input').fill(uniqueUsername)
    await page.getByTestId('displayName-input').fill('Test User')
    await page.getByTestId('password-input').fill('TestPassword123')

    const submitButton = page.getByTestId('submit-button')
    await submitButton.click()

    // Button should be disabled during submission
    await expect(submitButton).toBeDisabled()
    await expect(submitButton).toHaveText('Signing up...')

    // All inputs should be disabled
    await expect(page.getByTestId('username-input')).toBeDisabled()
    await expect(page.getByTestId('displayName-input')).toBeDisabled()
    await expect(page.getByTestId('bio-input')).toBeDisabled()
    await expect(page.getByTestId('password-input')).toBeDisabled()
  })

  test('navigation to signin page from signup', async ({ page }) => {
    await page.getByRole('link', { name: /sign in/i }).click()

    await expect(page).toHaveURL('/signin')
  })

  test('bio field is optional', async ({ page }) => {
    const uniqueUsername = `user${Date.now()}`

    await page.getByTestId('username-input').fill(uniqueUsername)
    await page.getByTestId('displayName-input').fill('Test User')
    await page.getByTestId('password-input').fill('TestPassword123')
    // Leave bio empty

    await page.getByTestId('submit-button').click()

    await expect(page).toHaveURL('/')
  })

  test('password field cleared after validation error', async ({ page }) => {
    await page.getByTestId('username-input').fill('ab')
    await page.getByTestId('password-input').fill('TestPassword123')
    await page.getByTestId('submit-button').click()

    await expect(page.getByTestId('username-error')).toBeVisible()
    await expect(page.getByTestId('password-input')).toHaveValue('')
  })

  test('non-password fields preserved after validation error', async ({
    page,
  }) => {
    await page.getByTestId('username-input').fill('testuser')
    await page.getByTestId('displayName-input').fill('Test User')
    await page.getByTestId('bio-input').fill('Test bio')
    await page.getByTestId('password-input').fill('short')

    await page.getByTestId('submit-button').click()

    await expect(page.getByTestId('password-error')).toBeVisible()
    await expect(page.getByTestId('username-input')).toHaveValue('testuser')
    await expect(page.getByTestId('displayName-input')).toHaveValue('Test User')
    await expect(page.getByTestId('bio-input')).toHaveValue('Test bio')
  })
})
