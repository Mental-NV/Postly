import { expect, test, type Page } from '@playwright/test'
import { signIn } from './helpers'

async function goToNotifications(page: Page): Promise<void> {
  await page.goto('/notifications')
  await expect(page.getByTestId('notifications-page')).toBeVisible()
}

async function createFollowNotification(page: Page, followerUsername: string, targetUsername: string): Promise<void> {
  await page.request.post(`/api/profiles/${targetUsername}/follow`, {
    headers: { 'Content-Type': 'application/json' },
  })
}

async function createLikeNotification(page: Page, postId: number): Promise<void> {
  await page.request.post(`/api/posts/${postId}/like`)
}

async function createReplyNotification(page: Page, postId: number, body: string): Promise<void> {
  await page.request.post(`/api/posts/${postId}/replies`, {
    data: { body },
  })
}

async function getBobPostId(page: Page): Promise<number> {
  const response = await page.request.get('/api/profiles/bob')
  const data = await response.json() as { posts: Array<{ id: number }> }
  return data.posts[0].id
}

test.describe('User Story 8: In-App Notifications', () => {
  // UF-08: Opening an available notification destination marks only that notification read
  test('UF-08: opening available notification marks only that notification read', async ({ page }) => {
    await signIn(page, { username: 'bob' })

    // Create multiple notifications for Bob
    await signIn(page, { username: 'alice' })
    const bobPostId = await getBobPostId(page)
    await createFollowNotification(page, 'alice', 'bob')
    await createLikeNotification(page, bobPostId)
    
    await signIn(page, { username: 'charlie' })
    await createReplyNotification(page, bobPostId, 'Charlie reply for notification test')

    // Sign back in as Bob and go to notifications
    await signIn(page, { username: 'bob' })
    await goToNotifications(page)

    // Verify multiple unread notifications visible
    await expect(page.getByTestId('notifications-list')).toBeVisible()
    const notifications = page.locator('[data-testid^="notification-item-"]')
    const count = await notifications.count()
    expect(count).toBeGreaterThanOrEqual(3)

    // Verify all are unread
    for (let i = 0; i < count; i++) {
      const notification = notifications.nth(i)
      const notificationId = (await notification.getAttribute('data-testid'))?.replace('notification-item-', '')
      await expect(page.getByTestId(`notification-unread-indicator-${notificationId}`)).toBeVisible()
    }

    // Click the first notification
    const firstNotification = notifications.first()
    const firstNotificationId = (await firstNotification.getAttribute('data-testid'))?.replace('notification-item-', '')
    await firstNotification.click()

    // Verify navigation occurred
    await expect(page).not.toHaveURL('/notifications')

    // Return to notifications
    await goToNotifications(page)

    // Verify only the clicked notification is marked read
    await expect(page.getByTestId(`notification-read-indicator-${firstNotificationId}`)).toBeVisible()

    // Verify other notifications remain unread
    const updatedNotifications = page.locator('[data-testid^="notification-item-"]')
    const updatedCount = await updatedNotifications.count()
    let unreadCount = 0
    for (let i = 0; i < updatedCount; i++) {
      const notification = updatedNotifications.nth(i)
      const notificationId = (await notification.getAttribute('data-testid'))?.replace('notification-item-', '')
      if (notificationId !== firstNotificationId) {
        const unreadIndicator = page.getByTestId(`notification-unread-indicator-${notificationId}`)
        if (await unreadIndicator.isVisible()) {
          unreadCount++
        }
      }
    }
    expect(unreadCount).toBeGreaterThan(0)
  })

  // UF-09: Opening an unavailable notification still lands on unavailable destination and marks it read
  test('UF-09: opening unavailable notification shows unavailable page and marks read', async ({ page }) => {
    await signIn(page, { username: 'bob' })

    // Create a post as Bob
    const createResponse = await page.request.post('/api/posts', {
      data: { body: 'Post to be deleted for unavailable notification test' },
    })
    const { post } = await createResponse.json() as { post: { id: number } }
    const postId = post.id

    // Alice likes the post (creates notification)
    await signIn(page, { username: 'alice' })
    await createLikeNotification(page, postId)

    // Bob deletes the post
    await signIn(page, { username: 'bob' })
    await page.request.delete(`/api/posts/${postId}`)

    // Go to notifications
    await goToNotifications(page)

    // Find the notification for the deleted post
    await expect(page.getByTestId('notifications-list')).toBeVisible()
    const notification = page.locator('[data-testid^="notification-item-"]').first()
    const notificationId = (await notification.getAttribute('data-testid'))?.replace('notification-item-', '')

    // Verify notification is unread
    await expect(page.getByTestId(`notification-unread-indicator-${notificationId}`)).toBeVisible()

    // Click the notification
    await notification.click()

    // Verify navigation to unavailable page
    await expect(page).toHaveURL('/notifications/unavailable')
    await expect(page.getByTestId('notification-unavailable-page')).toBeVisible()
    await expect(page.getByText('Content Not Available')).toBeVisible()

    // Return to notifications
    await page.getByRole('link', { name: /Back to Notifications/ }).click()
    await expect(page).toHaveURL('/notifications')

    // Verify notification is now marked read
    await expect(page.getByTestId(`notification-read-indicator-${notificationId}`)).toBeVisible()
  })

  // UF-10: Viewing the notifications list alone does not change unread state
  test('UF-10: viewing notifications list does not mark notifications read', async ({ page }) => {
    await signIn(page, { username: 'bob' })

    // Create unread notifications
    await signIn(page, { username: 'alice' })
    await createFollowNotification(page, 'alice', 'bob')

    // Sign back in as Bob
    await signIn(page, { username: 'bob' })
    await goToNotifications(page)

    // Verify notifications are unread
    await expect(page.getByTestId('notifications-list')).toBeVisible()
    const notifications = page.locator('[data-testid^="notification-item-"]')
    const count = await notifications.count()
    expect(count).toBeGreaterThan(0)

    const notificationIds: string[] = []
    for (let i = 0; i < count; i++) {
      const notification = notifications.nth(i)
      const notificationId = (await notification.getAttribute('data-testid'))?.replace('notification-item-', '')
      if (notificationId) {
        notificationIds.push(notificationId)
        await expect(page.getByTestId(`notification-unread-indicator-${notificationId}`)).toBeVisible()
      }
    }

    // Navigate away without clicking any notification
    await page.goto('/')
    await expect(page.getByTestId('timeline-page')).toBeVisible()

    // Return to notifications
    await goToNotifications(page)

    // Verify all notifications remain unread
    for (const notificationId of notificationIds) {
      await expect(page.getByTestId(`notification-unread-indicator-${notificationId}`)).toBeVisible()
    }
  })
})
