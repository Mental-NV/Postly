import { expect, test, type Locator, type Page } from '@playwright/test'
import { goToConversationPost, signIn } from './helpers'

function getPostCardByBody(page: Page, body: string): Locator {
  return page.locator('[data-testid^="post-card-"]', { hasText: body }).first()
}

async function getPostId(card: Locator): Promise<string> {
  const testId = await card.getAttribute('data-testid')
  if (testId == null) throw new Error('Expected post card test id to be present')
  return testId.replace('post-card-', '')
}

test.describe('User Story 7: Replies and conversation-oriented direct-post view', () => {
  // UF-04: Signed-in user creates a reply from the conversation route
  test('UF-04: creates a reply from the conversation route', async ({ page }) => {
    await signIn(page, { username: 'bob' })
    await goToConversationPost(page)

    await expect(page.getByTestId('conversation-target')).toBeVisible()
    await expect(page.getByTestId('reply-composer')).toBeVisible()

    const replyInput = page.getByTestId('reply-composer-input')
    await replyInput.fill('My e2e reply from Bob')
    await page.getByTestId('reply-submit-button').click()

    // Reply appears in conversation
    await expect(page.getByTestId('conversation-replies')).toContainText('My e2e reply from Bob')

    // Input is cleared
    await expect(replyInput).toHaveValue('')
  })

  // UF-05: Reply author edits and deletes their own reply, leaving a placeholder
  test('UF-05: author edits and deletes own reply, leaving a placeholder', async ({ page }) => {
    await signIn(page, { username: 'bob' })
    await goToConversationPost(page)

    // Create a reply
    await page.getByTestId('reply-composer-input').fill('Reply to edit and delete')
    await page.getByTestId('reply-submit-button').click()

    // Wait for reply to appear
    const replyCard = getPostCardByBody(page, 'Reply to edit and delete')
    await expect(replyCard).toBeVisible()
    const replyId = await getPostId(replyCard)

    // Edit the reply
    await page.getByTestId(`post-edit-button-${replyId}`).click()
    const editorTextarea = page.getByTestId(`post-editor-body-input-${replyId}`)
      .or(page.getByTestId('editor-textarea'))
    await editorTextarea.fill('Edited reply content')
    await page.getByTestId(`post-editor-save-button-${replyId}`)
      .or(page.getByTestId('editor-save'))
      .click()

    await expect(page.getByText('Edited reply content')).toBeVisible()

    // Delete the reply
    await page.getByTestId(`post-delete-button-${replyId}`).click()
    await expect(page.getByRole('dialog')).toBeVisible()
    await page.getByTestId('confirm-delete').click()

    // Placeholder appears instead of the reply
    await expect(page.getByTestId(`deleted-reply-placeholder-${replyId}`)).toBeVisible()
    await expect(page.getByText('Edited reply content')).not.toBeVisible()

    // No interactive controls on placeholder
    await expect(page.getByTestId(`post-edit-button-${replyId}`)).not.toBeVisible()
    await expect(page.getByTestId(`post-delete-button-${replyId}`)).not.toBeVisible()
  })

  // UF-06: Conversation stays open when the parent post is unavailable
  test('UF-06: conversation route remains accessible when parent post is unavailable', async ({ page }) => {
    await signIn(page, { username: 'bob' })

    // Create a fresh post to use as the conversation target so the shared seed post is preserved
    const createResponse = await page.request.post('/api/posts', {
      data: { body: 'UF-06 throwaway post' },
    })
    const { post } = await createResponse.json() as { post: { id: number } }
    const postId = post.id

    await page.goto(`/posts/${postId}`)
    await expect(page.getByTestId('conversation-page')).toBeVisible()

    // Delete the target post
    await page.getByTestId(`post-delete-button-${postId}`).click()
    await expect(page.getByRole('dialog')).toBeVisible()
    await page.getByTestId('confirm-delete').click()

    // Hard-delete navigates away; navigate back to verify 404 behavior
    await page.goto(`/posts/${postId}`)
    await expect(page.getByTestId('post-unavailable-state')).toBeVisible()
  })

  // UF-07: Non-authors are not offered reply edit/delete actions
  test('UF-07: non-author reply has no edit or delete controls', async ({ page }) => {
    await signIn(page, { username: 'bob' })
    await goToConversationPost(page)

    // Alice's seeded reply should be visible
    const aliceReplyCard = getPostCardByBody(page, "Alice's seeded reply on the conversation post")
    await expect(aliceReplyCard).toBeVisible()
    const aliceReplyId = await getPostId(aliceReplyCard)

    // No edit/delete controls for Alice's reply when Bob is signed in
    await expect(page.getByTestId(`post-edit-button-${aliceReplyId}`)).not.toBeVisible()
    await expect(page.getByTestId(`post-delete-button-${aliceReplyId}`)).not.toBeVisible()

    // Bob's own seeded reply should have controls
    const bobReplyCard = getPostCardByBody(page, "Bob's seeded reply on the conversation post")
    await expect(bobReplyCard).toBeVisible()
    const bobReplyId = await getPostId(bobReplyCard)

    await expect(page.getByTestId(`post-edit-button-${bobReplyId}`)).toBeVisible()
    await expect(page.getByTestId(`post-delete-button-${bobReplyId}`)).toBeVisible()
  })
})
