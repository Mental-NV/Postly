import { Buffer } from 'node:buffer'
import { expect, type FilePayload, type Page } from '@playwright/test'

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
  await expect(page).toHaveURL('/')
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
