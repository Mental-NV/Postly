import { expect, type Page } from '@playwright/test'

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
