import { expect, test, type Page } from '@playwright/test'
import { signIn } from './helpers'

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
    await signIn(page, { username: 'bob' })

    await page.goto('/u/alice')
    await expect(page.getByTestId('profile-page')).toBeVisible()
    await expect(page.getByTestId('profile-display-name')).toHaveText('Alice Example')

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

  test('signed-out visitor can view public profile and direct post in read-only mode', async ({ page }) => {
    await page.goto('/u/alice')
    const publicLikeCounts = page.locator('[data-testid^="post-like-count-"]')

    await expect(page).toHaveURL('/u/alice')
    await expect(page.getByTestId('profile-page')).toBeVisible()
    await expect(page.getByTestId('profile-display-name')).toHaveText('Alice Example')
    await expect(page.getByTestId('follow-unfollow-button')).toHaveCount(0)
    await expect(page.locator('[data-testid^="post-like-button-"]')).toHaveCount(0)
    expect(await publicLikeCounts.count()).toBeGreaterThan(0)

    await page.locator('[data-testid^="post-permalink-"]').first().click()

    await expect(page).toHaveURL(/\/posts\/\d+$/)
    await expect(page.getByTestId('post-page')).toBeVisible()
    await expect(page.locator('[data-testid^="post-like-button-"]')).toHaveCount(0)
    await expect(page.locator('[data-testid^="post-edit-button-"]')).toHaveCount(0)
    await expect(page.locator('[data-testid^="post-delete-button-"]')).toHaveCount(0)

    await page.getByTestId('brand-link').click()
    await expect(page).toHaveURL('/signin?returnUrl=%2F')
  })
})
