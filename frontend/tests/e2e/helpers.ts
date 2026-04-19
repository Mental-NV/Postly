import { Buffer } from 'node:buffer'
import * as fs from 'node:fs/promises'
import * as path from 'node:path'
import { fileURLToPath } from 'node:url'
import { expect, type FilePayload, type Locator, type Page } from '@playwright/test'

const __filename = fileURLToPath(import.meta.url)
const __dirname = path.dirname(__filename)

export async function getAssetAvatar001(): Promise<FilePayload> {
  const assetPath = path.resolve(
    __dirname,
    '../../../backend/tests/assets/avatars/001.jpg'
  )
  const buffer = await fs.readFile(assetPath)
  return {
    name: '001.jpg',
    mimeType: 'image/jpeg',
    buffer,
  }
}

export async function signIn(
  page: Page,
  {
    username = 'alice',
    password = 'TestPassword123',
  }: { username?: string; password?: string } = {}
) {
  await page.goto('/signin')
  await page.getByTestId('username-input').fill(username)
  await page.getByTestId('password-input').fill(password)
  await page.getByTestId('submit-button').click()
  await expect(page).toHaveURL('/', { timeout: 15000 })
  await expect(page.getByTestId('timeline-feed')).toBeVisible({
    timeout: 15000,
  })
}

/** Navigate to the seeded conversation post (Alice's ConversationPostBody post). */
export async function goToConversationPost(page: Page): Promise<void> {
  await page.goto('/u/bob')
  await expect(page.getByTestId('profile-page')).toBeVisible()
  const conversationCard = page
    .locator('[data-testid^="post-card-"]', { hasText: 'Seed conversation post for reply flows' })
    .first()
  const postId = (await conversationCard.getAttribute('data-testid'))?.replace('post-card-', '')
  if (!postId) throw new Error('Could not find conversation post card')
  await page.goto(`/posts/${postId}`)
  await expect(page.getByTestId('conversation-page')).toBeVisible()
}

export async function createCanvasPngUpload(
  page: Page,
  {
    width = 320,
    height = 320,
    color = '#00ba7c',
    transparent = false,
    name = `avatar-${width}x${height}.png`,
  }: {
    width?: number
    height?: number
    color?: string
    transparent?: boolean
    name?: string
  } = {}
): Promise<FilePayload> {
  const dataUrl = await page.evaluate(
    ({ width, height, color, transparent }) => {
      const canvas = document.createElement('canvas')
      canvas.width = width
      canvas.height = height

      const context = canvas.getContext('2d')
      if (context == null) {
        throw new Error('Canvas 2D context is unavailable')
      }

      if (!transparent) {
        context.fillStyle = '#ffffff'
        context.fillRect(0, 0, width, height)
      }

      const inset = Math.max(24, Math.floor(Math.min(width, height) * 0.12))
      context.fillStyle = color
      context.fillRect(inset, inset, width - inset * 2, height - inset * 2)

      return canvas.toDataURL('image/png')
    },
    { width, height, color, transparent }
  )

  return {
    name,
    mimeType: 'image/png',
    buffer: Buffer.from(dataUrl.replace(/^data:image\/png;base64,/, ''), 'base64'),
  }
}

export function createInvalidSvgUpload(
  name = 'avatar.svg'
): FilePayload {
  return {
    name,
    mimeType: 'image/svg+xml',
    buffer: Buffer.from(
      '<svg xmlns="http://www.w3.org/2000/svg" width="320" height="320"><rect width="320" height="320" fill="#111827"/></svg>'
    ),
  }
}

export async function goToNotifications(page: Page): Promise<void> {
  await page.goto('/notifications')
  await expect(page.getByTestId('notifications-page')).toBeVisible()
}

export async function createNotificationViaFollow(
  page: Page,
  targetUsername: string
): Promise<void> {
  await page.request.post(`/api/profiles/${targetUsername}/follow`)
}

export async function createNotificationViaLike(
  page: Page,
  postId: number
): Promise<void> {
  await page.request.post(`/api/posts/${postId}/like`)
}

export async function createNotificationViaReply(
  page: Page,
  postId: number,
  body: string
): Promise<void> {
  await page.request.post(`/api/posts/${postId}/replies`, {
    data: { body },
  })
}

export async function failNextContinuationRequestOnce(
  page: Page,
  matcher: string | RegExp,
  {
    status = 500,
    type = 'CONTINUATION_FAILED',
    title = 'Unable to load more content',
    detail = 'Please try again.',
  }: {
    status?: number
    type?: string
    title?: string
    detail?: string
  } = {}
): Promise<void> {
  let hasFailed = false

  await page.route(matcher, async (route) => {
    const requestUrl = route.request().url()
    if (!hasFailed && requestUrl.includes('cursor=')) {
      hasFailed = true
      await route.fulfill({
        status,
        contentType: 'application/problem+json',
        body: JSON.stringify({
          type,
          title,
          detail,
        }),
      })
      return
    }

    await route.continue()
  })
}
