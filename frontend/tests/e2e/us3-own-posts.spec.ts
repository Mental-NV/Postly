import { test, expect } from '@playwright/test'

test.describe('User Story 3: Own Posts', () => {
  test.beforeEach(async ({ page }) => {
    // Sign in as bob
    await page.goto('/signin')
    await page.getByTestId('username-input').fill('bob')
    await page.getByTestId('password-input').fill('TestPassword123')
    await page.getByTestId('submit-button').click()
    await expect(page).toHaveURL('/')

    // Wait for timeline page to load
    await expect(page.getByRole('heading', { name: 'Home' })).toBeVisible()
    await expect(page.getByTestId('composer-textarea')).toBeVisible()
  })

  test('create post with character counter', async ({ page }) => {
    const textarea = page.getByTestId('composer-textarea')
    const charCounter = page.getByTestId('composer-char-counter')
    const postBody = `Hello world! ${Date.now()}`

    // Type and verify counter
    await textarea.fill(postBody)
    await expect(charCounter).toHaveText(String(280 - postBody.length))

    // Submit
    await page.getByTestId('composer-submit').click()

    // Verify textarea is cleared
    await expect(textarea).toHaveValue('')
    await expect(page.getByText(postBody)).toBeVisible()
  })

  test('character limit validation', async ({ page }) => {
    const textarea = page.getByTestId('composer-textarea')
    const submitButton = page.getByTestId('composer-submit')
    const charCounter = page.getByTestId('composer-char-counter')

    // Empty post - submit disabled
    await expect(submitButton).toBeDisabled()

    // Valid post - submit enabled
    await textarea.fill('Valid post')
    await expect(submitButton).toBeEnabled()
    await expect(charCounter).toHaveText('270')

    // Over limit - submit disabled and counter goes negative
    await textarea.fill('a'.repeat(281))
    await expect(submitButton).toBeDisabled()
    await expect(charCounter).toHaveText('-1')
  })

  test('draft preservation on error', async ({ page }) => {
    const textarea = page.getByTestId('composer-textarea')
    const submitButton = page.getByTestId('composer-submit')
    const charCounter = page.getByTestId('composer-char-counter')

    await page.route('**/api/posts', async (route) => {
      await route.fulfill({
        status: 500,
        contentType: 'application/problem+json',
        body: JSON.stringify({
          type: 'SERVER_ERROR',
          title: 'Server error',
          detail: 'Failed to create post',
        }),
      })
    }, { times: 1 })

    await textarea.fill('Test post content')
    await submitButton.click()

    // Verify textarea preserves content
    await expect(textarea).toHaveValue('Test post content')
    await expect(page.getByRole('alert')).toContainText('Failed to create post')
    await expect(charCounter).toHaveText('263')
  })

  test('composer shows correct initial state', async ({ page }) => {
    const textarea = page.getByTestId('composer-textarea')
    const submitButton = page.getByTestId('composer-submit')

    // Initial state
    await expect(textarea).toHaveValue('')
    await expect(textarea).toHaveAttribute('placeholder', "What's happening?")
    await expect(submitButton).toBeDisabled()
  })

  test('character counter updates in real-time', async ({ page }) => {
    const textarea = page.getByTestId('composer-textarea')
    const charCounter = page.getByTestId('composer-char-counter')

    await textarea.fill('H')
    await expect(charCounter).toHaveText('279')

    await textarea.fill('He')
    await expect(charCounter).toHaveText('278')

    await textarea.fill('Hello')
    await expect(charCounter).toHaveText('275')
  })

  test('submit button shows pending state', async ({ page }) => {
    const textarea = page.getByTestId('composer-textarea')
    const submitButton = page.getByTestId('composer-submit')
    const routeDelayMs = 300

    await textarea.fill('Test post')
    await expect(submitButton).toBeEnabled()

    await page.route('**/api/posts', async (route) => {
      await page.waitForTimeout(routeDelayMs)
      await route.continue()
    }, { times: 1 })

    // Click submit button
    await submitButton.click()

    await expect(submitButton).toBeDisabled()
    await expect(submitButton).toHaveText('Posting...')

    // Verify textarea is cleared after successful submission
    await expect(textarea).toHaveValue('')
  })
})
