import { expect, test, type Page } from '@playwright/test'

async function signInAsBob(page: Page) {
  await page.goto('/signin')
  await page.getByTestId('username-input').fill('bob')
  await page.getByTestId('password-input').fill('TestPassword123')
  await page.getByTestId('submit-button').click()
  await expect(page).toHaveURL('/')
}

function readLikeCount(locatorText: string | null) {
  const match = locatorText?.match(/\d+/)
  return match == null ? 0 : Number(match[0])
}

async function expectLikeState(
  button: ReturnType<Page['locator']>,
  count: ReturnType<Page['locator']>,
  expectedPressed: boolean,
  expectedCount: number
) {
  await expect(button).toHaveAttribute(
    'aria-pressed',
    expectedPressed ? 'true' : 'false'
  )

  if (expectedCount === 0) {
    await expect(count).toHaveText('')
  } else {
    await expect(count).toHaveText(String(expectedCount))
  }
}

test.describe('User Story 5: Likes and direct post', () => {
  test('likes or unlikes from profile, opens direct post, and toggles again there', async ({ page }) => {
    await signInAsBob(page)

    await page.goto('/u/alice')
    await expect(page.locator('.profile-page')).toBeVisible()
    await expect(page.locator('.profile-display-name')).toHaveText('Alice Example')

    const likeButton = page.locator('[data-testid^="post-like-button-"]').first()
    const likeCount = page.locator('[data-testid^="post-like-count-"]').first()
    const permalink = page.locator('[data-testid^="post-permalink-"]').first()

    await expect(likeButton).toBeVisible()
    await expect(permalink).toBeVisible()

    const profileLiked =
      (await likeButton.getAttribute('aria-pressed')) === 'true'
    const profileCountBefore = await readLikeCount(await likeCount.textContent())

    await likeButton.click()

    await expectLikeState(
      likeButton,
      likeCount,
      !profileLiked,
      profileCountBefore + (profileLiked ? -1 : 1)
    )

    await permalink.click()
    await expect(page).toHaveURL(/\/posts\/\d+$/)
    await expect(page.getByTestId('post-page')).toBeVisible()

    const directLikeButton = page.locator('[data-testid^="post-like-button-"]').first()
    const directLikeCount = page.locator('[data-testid^="post-like-count-"]').first()

    await expect(directLikeButton).toBeVisible()

    const directLiked =
      (await directLikeButton.getAttribute('aria-pressed')) === 'true'
    const directCountBefore = await readLikeCount(await directLikeCount.textContent())

    await directLikeButton.click()

    await expectLikeState(
      directLikeButton,
      directLikeCount,
      !directLiked,
      directCountBefore + (directLiked ? -1 : 1)
    )
  })

  test('signed-out visitor returns to an unavailable direct post after sign-in', async ({ page }) => {
    await page.goto('/posts/999999')

    await expect(page).toHaveURL(/\/signin\?returnUrl=/)

    await page.getByTestId('username-input').fill('bob')
    await page.getByTestId('password-input').fill('TestPassword123')
    await page.getByTestId('submit-button').click()

    await expect(page).toHaveURL('/posts/999999')
    await expect(page.getByTestId('post-unavailable-state')).toBeVisible()

    await page.getByTestId('post-unavailable-home-link').click()
    await expect(page).toHaveURL('/')
  })
})
