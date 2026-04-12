import { test, expect } from '@playwright/test'
import { signIn } from './helpers'

test.describe('User Story 3: Own Posts', () => {
  test.beforeEach(async ({ page }) => {
    await signIn(page, { username: 'bob' })

    // Wait for timeline page to load
    await expect(page.getByRole('heading', { name: 'Home' })).toBeVisible()
    await expect(page.getByTestId('composer-textarea')).toBeVisible()
  })

  test('creates a post and shows it on the timeline', async ({ page }) => {
    const textarea = page.getByTestId('composer-textarea')
    const postBody = `Hello world! ${Date.now()}`

    await textarea.fill(postBody)

    await page.getByTestId('composer-submit').click()

    await expect(textarea).toHaveValue('')
    await expect(page.getByText(postBody)).toBeVisible()
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
})
