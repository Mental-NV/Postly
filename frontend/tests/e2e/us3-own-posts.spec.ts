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
    await expect(page.getByRole('heading', { name: 'Timeline' })).toBeVisible()
  })

  test('create post with character counter', async ({ page }) => {
    const textarea = page.getByTestId('composer-textarea')

    // Type and verify counter
    await textarea.fill('Hello world!')
    await expect(page.getByText('12/280')).toBeVisible()

    // Submit
    await page.getByTestId('composer-submit').click()

    // Wait for success alert
    page.once('dialog', (dialog) => {
      expect(dialog.message()).toBe('Post created successfully!')
      dialog.accept()
    })

    // Verify textarea is cleared
    await expect(textarea).toHaveValue('')
  })

  test('character limit validation', async ({ page }) => {
    const textarea = page.getByTestId('composer-textarea')
    const submitButton = page.getByTestId('composer-submit')

    // Empty post - submit disabled
    await expect(submitButton).toBeDisabled()

    // Valid post - submit enabled
    await textarea.fill('Valid post')
    await expect(submitButton).toBeEnabled()
    await expect(page.getByText('10/280')).toBeVisible()

    // Over limit - submit disabled and error shown
    await textarea.fill('a'.repeat(281))
    await expect(submitButton).toBeDisabled()
    await expect(page.getByText('281/280')).toBeVisible()
    await expect(page.getByText(/exceeds 280 character limit/i)).toBeVisible()
  })

  test('draft preservation on error', async ({ page }) => {
    const textarea = page.getByTestId('composer-textarea')

    await textarea.fill('Test post content')

    // Verify textarea preserves content
    await expect(textarea).toHaveValue('Test post content')
    await expect(page.getByText('17/280')).toBeVisible()
  })

  test('composer shows correct initial state', async ({ page }) => {
    const textarea = page.getByTestId('composer-textarea')
    const submitButton = page.getByTestId('composer-submit')

    // Initial state
    await expect(textarea).toHaveValue('')
    await expect(textarea).toHaveAttribute('placeholder', "What's happening?")
    await expect(submitButton).toBeDisabled()
    await expect(page.getByText('0/280')).toBeVisible()
  })

  test('character counter updates in real-time', async ({ page }) => {
    const textarea = page.getByTestId('composer-textarea')

    await textarea.fill('H')
    await expect(page.getByText('1/280')).toBeVisible()

    await textarea.fill('He')
    await expect(page.getByText('2/280')).toBeVisible()

    await textarea.fill('Hello')
    await expect(page.getByText('5/280')).toBeVisible()
  })

  test('submit button shows pending state', async ({ page }) => {
    const textarea = page.getByTestId('composer-textarea')
    const submitButton = page.getByTestId('composer-submit')

    await textarea.fill('Test post')
    await expect(submitButton).toBeEnabled()

    // Click submit button
    await submitButton.click()

    // Wait for success alert (indicates submission completed)
    page.once('dialog', (dialog) => {
      expect(dialog.message()).toBe('Post created successfully!')
      dialog.accept()
    })

    // Verify textarea is cleared after successful submission
    await expect(textarea).toHaveValue('')
  })
})
