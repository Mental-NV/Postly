import { expect, test, type Locator, type Page } from '@playwright/test'
import {
  failNextContinuationRequestOnce,
  goToConversationPost,
  signIn,
} from './helpers'

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
    .poll(async () => cards.count())
    .toBeGreaterThan(initialCount)
}

test.describe('User Story 9: Automatic continuation loading', () => {
  test('UF-11: home timeline automatically loads additional items at the continuation point', async ({
    page,
  }) => {
    await signIn(page, { username: 'bob' })
    await page.goto('/')

    const feed = page.getByTestId('timeline-feed')
    await expect(feed).toBeVisible()

    const cards = feed.locator('[data-testid^="post-card-"]')
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
    await failNextContinuationRequestOnce(page, /\/api\/profiles\/alice(\?.*)?$/)

    await page.goto('/u/alice')
    await expect(page.getByTestId('profile-page')).toBeVisible()

    const cards = page
      .getByTestId('profile-posts')
      .locator('[data-testid^="post-card-"]')
    const initialIds = await getVisibleCardIds(cards)
    const initialCount = initialIds.length

    await page.getByTestId('collection-continuation-sentinel').scrollIntoViewIfNeeded()

    await expect(page.getByTestId('collection-continuation-error')).toBeVisible()
    expect(await cards.count()).toBe(initialCount)

    for (const cardId of initialIds) {
      await expect(page.getByTestId(cardId)).toBeVisible()
    }

    await page.getByTestId('collection-continuation-retry').click()

    await expectCountToIncrease(cards, initialCount)
    await expect(page.getByTestId('collection-end-state')).toBeVisible()

    const visibleIds = await getVisibleCardIds(cards)
    expect(new Set(visibleIds).size).toBe(visibleIds.length)
  })

  test('UF-13: conversation continuation reaches an explicit end-of-list state', async ({
    page,
  }) => {
    await signIn(page, { username: 'bob' })
    await goToConversationPost(page)

    const replies = page
      .getByTestId('conversation-replies')
      .locator('[data-testid^="post-card-"]')
    const initialCount = await replies.count()
    expect(initialCount).toBeGreaterThanOrEqual(20)

    await page.getByTestId('collection-continuation-sentinel').scrollIntoViewIfNeeded()

    await expectCountToIncrease(replies, initialCount)
    await expect(page.getByTestId('collection-end-state')).toBeVisible()
    await expect(page.getByTestId('conversation-replies')).toBeVisible()
    await expect(page.getByText(/no replies yet/i)).toHaveCount(0)

    const visibleIds = await getVisibleCardIds(replies)
    expect(new Set(visibleIds).size).toBe(visibleIds.length)
  })
})
