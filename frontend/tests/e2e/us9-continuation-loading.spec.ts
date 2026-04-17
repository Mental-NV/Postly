import { expect, test, type Locator, type Page } from '@playwright/test'
import {
  signIn,
} from './helpers'

interface CreatedPostResponse {
  post: {
    id: number
  }
}

async function getVisibleCardIds(cards: Locator): Promise<string[]> {
  const count = await cards.count()
  const ids: string[] = []

  for (let index = 0; index < count; index += 1) {
    const testId = await cards.nth(index).getAttribute('data-testid')
    if (testId != null) {
      ids.push(testId)
    }
  }

  return ids
}

async function expectCountToIncrease(cards: Locator, initialCount: number): Promise<void> {
  await expect
    .poll(async () => cards.count(), { timeout: 15000 })
    .toBeGreaterThan(initialCount)
}

async function createDedicatedConversationThread(
  page: Page,
  replyCount = 22
): Promise<number> {
  const timestamp = Date.now()
  const createPostResponse = await page.request.post('/api/posts', {
    data: { body: `UF-13 dedicated conversation ${timestamp}` },
  })
  expect(createPostResponse.ok()).toBeTruthy()

  const { post } = await createPostResponse.json() as CreatedPostResponse

  for (let index = 1; index <= replyCount; index += 1) {
    const replyResponse = await page.request.post(`/api/posts/${post.id}/replies`, {
      data: { body: `UF-13 reply ${timestamp}-${index}` },
    })
    expect(replyResponse.ok()).toBeTruthy()
  }

  return post.id
}

test.describe('User Story 9: Automatic continuation loading', () => {
  test('UF-11: home timeline automatically loads additional items at the continuation point', async ({
    page,
  }) => {
    await signIn(page, { username: 'bob' })

    const feed = page.getByTestId('timeline-feed')
    await expect(feed).toBeVisible()

    const cards = feed.locator('[data-testid^="post-card-"]')
    await expect
      .poll(async () => cards.count(), { timeout: 15000 })
      .toBeGreaterThanOrEqual(20)
    const initialCount = await cards.count()
    expect(initialCount).toBeGreaterThanOrEqual(20)

    await page.getByTestId('collection-continuation-sentinel').scrollIntoViewIfNeeded()

    await expectCountToIncrease(cards, initialCount)
    await expect(page.getByTestId('collection-end-state')).toBeVisible()

    const visibleIds = await getVisibleCardIds(cards)
    expect(new Set(visibleIds).size).toBe(visibleIds.length)
  })

  test('UF-12: profile continuation failure preserves visible items and recovers through retry', async ({
    page,
  }) => {
    await signIn(page, { username: 'bob' })
    const profilePage = await page.context().newPage()

    await profilePage.goto('/u/alice')
    await expect(profilePage.getByTestId('profile-page')).toBeVisible()

    const cards = profilePage
      .getByTestId('profile-posts')
      .locator('[data-testid^="post-card-"]')
    await expect
      .poll(async () => cards.count(), { timeout: 15000 })
      .toBeGreaterThan(0)
    const initialIds = await getVisibleCardIds(cards)
    const initialCount = initialIds.length

    await profilePage.context().setOffline(true)
    await profilePage.getByTestId('collection-continuation-sentinel').scrollIntoViewIfNeeded()

    await expect(profilePage.getByTestId('collection-continuation-error')).toBeVisible()
    expect(await cards.count()).toBe(initialCount)

    for (const cardId of initialIds) {
      await expect(profilePage.getByTestId(cardId)).toBeVisible()
    }

    await profilePage.context().setOffline(false)
    const retryButton = profilePage.getByTestId('collection-continuation-retry')
    await expect(retryButton).toBeVisible()
    await retryButton.dispatchEvent('click')

    await expectCountToIncrease(cards, initialCount)
    await expect(profilePage.getByTestId('collection-end-state')).toBeVisible()

    const visibleIds = await getVisibleCardIds(cards)
    expect(new Set(visibleIds).size).toBe(visibleIds.length)
  })

  test('UF-13: conversation continuation reaches an explicit end-of-list state', async ({
    page,
  }) => {
    await signIn(page, { username: 'bob' })
    const postId = await createDedicatedConversationThread(page)
    await page.goto(`/posts/${postId}`)
    await expect(page.getByTestId('conversation-page')).toBeVisible()

    const replies = page
      .getByTestId('conversation-replies')
      .locator('[data-testid^="post-card-"]')
    await expect
      .poll(async () => replies.count(), { timeout: 15000 })
      .toBe(20)
    const initialCount = await replies.count()
    expect(initialCount).toBe(20)

    await page.getByTestId('collection-continuation-sentinel').scrollIntoViewIfNeeded()

    await expectCountToIncrease(replies, initialCount)
    await expect(page.getByTestId('collection-end-state')).toBeVisible()
    await expect(page.getByTestId('conversation-replies')).toBeVisible()
    await expect(page.getByText(/no replies yet/i)).toHaveCount(0)

    const visibleIds = await getVisibleCardIds(replies)
    expect(new Set(visibleIds).size).toBe(visibleIds.length)
  })
})
