import { expect, test, type Locator, type Page } from '@playwright/test'
import {
  createCanvasPngUpload,
  createInvalidSvgUpload,
  signIn,
} from './helpers'

async function getPostId(card: Locator): Promise<string> {
  const testId = await card.getAttribute('data-testid')
  if (testId == null) {
    throw new Error('Expected post card test id to be present')
  }

  return testId.replace('post-card-', '')
}

function getPostCardByBody(page: Page, body: string): Locator {
  return page.locator('[data-testid^="post-card-"]', { hasText: body }).first()
}

async function expectFallbackAvatar(
  avatarWrapper: Locator,
  expectedInitials: string
): Promise<void> {
  await expect(avatarWrapper.locator('img')).toHaveCount(0)
  await expect(avatarWrapper).toContainText(expectedInitials)
}

test.describe('User Story 6: Profile editing and avatar replacement', () => {
  test('uses the generated fallback avatar on profile, timeline, and conversation surfaces', async ({
    page,
  }) => {
    await signIn(page, { username: 'bob' })

    await page.goto('/u/bob')
    await expect(page.getByTestId('profile-page')).toBeVisible()
    await expect(page.getByTestId('profile-display-name')).toHaveText(
      'Bob Tester'
    )
    await expect(page.getByTestId('profile-avatar-fallback')).toHaveText('BT')

    await page.goto('/')
    await expect(page.getByTestId('timeline-feed')).toBeVisible()

    const bobTimelineCard = getPostCardByBody(page, 'Seed post from Bob')
    const bobPostId = await getPostId(bobTimelineCard)
    const timelineAvatar = page.getByTestId(`post-avatar-${bobPostId}`)

    await expectFallbackAvatar(timelineAvatar, 'BT')

    await bobTimelineCard
      .getByTestId(`post-permalink-${bobPostId}`)
      .click()

    await expect(page.getByTestId('conversation-page')).toBeVisible()
    await expectFallbackAvatar(page.getByTestId(`post-avatar-${bobPostId}`), 'BT')
  })

  test('saves valid profile changes, refreshes visible identity surfaces, and preserves saved identity after invalid edits', async ({
    page,
  }) => {
    await signIn(page, { username: 'bob' })

    await page.goto('/u/bob')
    await expect(page.getByTestId('profile-page')).toBeVisible()

    await page.getByTestId('profile-edit-button').click()
    await page.getByTestId('profile-display-name-input').fill('Bob Refreshed')
    await page
      .getByTestId('profile-bio-input')
      .fill('Updated profile bio for the profile editing story.')
    await page.getByTestId('profile-avatar-input').setInputFiles(
      await createCanvasPngUpload(page, {
        width: 320,
        height: 400,
        transparent: true,
      })
    )
    await page.getByTestId('profile-save-button').click()

    await expect(page.getByTestId('profile-form-status')).toHaveText(
      'Profile saved.'
    )
    await expect(page.getByTestId('profile-display-name')).toHaveText(
      'Bob Refreshed'
    )
    await expect(page.getByTestId('profile-bio')).toHaveText(
      'Updated profile bio for the profile editing story.'
    )
    await expect(page.getByTestId('profile-avatar-image')).toBeVisible()

    const savedAvatarUrl = await page
      .getByTestId('profile-avatar-image')
      .getAttribute('src')

    expect(savedAvatarUrl).toMatch(/\/api\/profiles\/bob\/avatar\?v=\d+/)

    await page.goto('/')
    await expect(page.getByTestId('timeline-feed')).toBeVisible()

    const bobTimelineCard = getPostCardByBody(page, 'Seed post from Bob')
    const bobPostId = await getPostId(bobTimelineCard)

    await expect(bobTimelineCard).toContainText('Bob Refreshed')
    await expect(
      page.getByTestId(`post-avatar-${bobPostId}`).locator('img')
    ).toHaveAttribute('src', savedAvatarUrl ?? '')

    await bobTimelineCard.getByTestId(`post-permalink-${bobPostId}`).click()

    await expect(page.getByTestId('conversation-page')).toBeVisible()
    await expect(page.getByText('Bob Refreshed').first()).toBeVisible()
    await expect(
      page.getByTestId(`post-avatar-${bobPostId}`).locator('img')
    ).toHaveAttribute('src', savedAvatarUrl ?? '')

    await page.goto('/u/bob')
    await page.getByTestId('profile-edit-button').click()

    const invalidBio = 'x'.repeat(161)
    await page.getByTestId('profile-display-name-input').fill('   ')
    await page.getByTestId('profile-bio-input').fill(invalidBio)
    await page
      .getByTestId('profile-avatar-input')
      .setInputFiles(createInvalidSvgUpload())
    await page.getByTestId('profile-save-button').click()

    await expect(page.getByTestId('profile-form-status')).toHaveText(
      'Resolve the highlighted profile fields and try again.'
    )
    await expect(page.getByText(/Display name must be between 1 and 50/)).toBeVisible()
    await expect(page.getByText('Bio must be 160 characters or fewer.')).toBeVisible()
    await expect(
      page.getByText('Avatar upload must be a still JPEG or PNG image.')
    ).toBeVisible()
    await expect(page.getByTestId('profile-display-name-input')).toHaveValue(
      '   '
    )
    await expect(page.getByTestId('profile-bio-input')).toHaveValue(invalidBio)
    await expect(page.getByTestId('profile-display-name')).toHaveText(
      'Bob Refreshed'
    )
    await expect(page.getByTestId('profile-avatar-image')).toHaveAttribute(
      'src',
      savedAvatarUrl ?? ''
    )
  })
})
