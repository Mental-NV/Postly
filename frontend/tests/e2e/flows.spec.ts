import { expect, test, type Page } from '@playwright/test';

// These tests intentionally mirror the normative flows in
// ../../specs/001-microblog-mvp/user-flows.md.
// They are a contract scaffold: the frontend implementation should satisfy
// these routes, roles, and test IDs.

const users = {
  alice: { username: 'alice', password: 'TestPass123!' },
  bob: { username: 'bob', password: 'TestPass123!' },
} as const;

const seedData = {
  aliceVisiblePost: 'Seed post from Alice',
} as const;

function uniqueUsername(prefix: string) {
  return `${prefix}_${Date.now()}`;
}

async function signIn(page: Page, username = users.bob.username, password = users.bob.password) {
  await page.goto('/signin');
  await page.getByTestId('signin-username-input').fill(username);
  await page.getByTestId('signin-password-input').fill(password);
  await page.getByTestId('signin-submit-button').click();
  await expect(page.getByTestId('home-page')).toBeVisible();
}

function postCardByText(page: Page, body: string) {
  return page.locator('[data-testid^="post-card-"]').filter({ hasText: body }).first();
}

async function createPost(page: Page, body: string) {
  await page.getByTestId('composer-body-input').fill(body);
  await page.getByTestId('composer-submit-button').click();
  await expect(postCardByText(page, body)).toBeVisible();
}

test.describe('Postly MVP flows', () => {
  test('UF-01 sign up creates an account and lands on home', async ({ page }) => {
    const username = uniqueUsername('new_user');

    await page.goto('/signup');

    await expect(page.getByTestId('signup-heading')).toBeVisible();
    await page.getByTestId('signup-username-input').fill(username);
    await page.getByTestId('signup-display-name-input').fill('New User');
    await page.getByTestId('signup-bio-input').fill('Hello from Playwright');
    await page.getByTestId('signup-password-input').fill(users.bob.password);
    await page.getByTestId('signup-submit-button').click();

    await expect(page).toHaveURL(/\/$/);
    await expect(page.getByTestId('home-page')).toBeVisible();
  });

  test('UF-02 sign up validation keeps the user on the form', async ({ page }) => {
    await page.goto('/signup');

    await page.getByTestId('signup-submit-button').click();

    await expect(page).toHaveURL(/\/signup$/);
    await expect(page.getByTestId('signup-form')).toBeVisible();
  });

  test('UF-03 sign in sends a returning user to home', async ({ page }) => {
    await signIn(page);
  });

  test('UF-04 protected redirect returns the user to the requested profile', async ({ page }) => {
    await page.goto(`/u/${users.alice.username}`);

    await expect(page).toHaveURL(/\/signin$/);
    await expect(page.getByTestId('signin-redirect-message')).toBeVisible();

    await page.getByTestId('signin-username-input').fill(users.bob.username);
    await page.getByTestId('signin-password-input').fill(users.bob.password);
    await page.getByTestId('signin-submit-button').click();

    await expect(page).toHaveURL(new RegExp(`/u/${users.alice.username}$`));
    await expect(page.getByTestId('profile-page')).toBeVisible();
  });

  test('UF-05 publish post shows the new post in home and profile', async ({ page }) => {
    const body = `My first Postly post ${Date.now()}`;

    await signIn(page);

    await createPost(page, body);

    await expect(postCardByText(page, body)).toBeVisible();

    await page.getByTestId('nav-profile-link').click();
    await expect(postCardByText(page, body)).toBeVisible();
  });

  test('UF-06 edit own post updates the body and shows edited state', async ({ page }) => {
    const originalBody = `Editable Postly post ${Date.now()}`;
    const updatedBody = `${originalBody} updated`;

    await signIn(page);
    await createPost(page, originalBody);

    const card = postCardByText(page, originalBody);

    await card.locator('[data-testid^="post-edit-button-"]').click();
    await card.locator('[data-testid^="post-editor-body-input-"]').fill(updatedBody);
    await card.locator('[data-testid^="post-editor-save-button-"]').click();

    const updatedCard = postCardByText(page, updatedBody);
    await expect(updatedCard).toBeVisible();
    await expect(updatedCard.locator('[data-testid^="post-edited-badge-"]')).toBeVisible();
  });

  test('UF-07 delete own post removes it from the visible list', async ({ page }) => {
    const body = `Disposable Postly post ${Date.now()}`;

    await signIn(page);
    await createPost(page, body);

    const card = postCardByText(page, body);
    await card.locator('[data-testid^="post-delete-button-"]').click();
    await page.getByTestId('confirm-dialog-confirm').click();

    await expect(postCardByText(page, body)).toHaveCount(0);
  });

  test('UF-08 follow adds relationship state and followed posts to home', async ({ page }) => {
    await signIn(page);
    await page.goto(`/u/${users.alice.username}`);

    await page.getByTestId('profile-follow-button').click();
    await expect(page.getByTestId('profile-unfollow-button')).toBeVisible();

    await page.getByTestId('nav-home-link').click();
    await expect(postCardByText(page, seedData.aliceVisiblePost)).toBeVisible();
  });

  test('UF-09 unfollow removes followed content from home', async ({ page }) => {
    await signIn(page);
    await page.goto(`/u/${users.alice.username}`);

    if (await page.getByTestId('profile-follow-button').isVisible().catch(() => false)) {
      await page.getByTestId('profile-follow-button').click();
      await expect(page.getByTestId('profile-unfollow-button')).toBeVisible();
    }

    await page.getByTestId('profile-unfollow-button').click();
    await expect(page.getByTestId('profile-follow-button')).toBeVisible();

    await page.getByTestId('nav-home-link').click();
    await expect(postCardByText(page, seedData.aliceVisiblePost)).toHaveCount(0);
  });

  test('UF-10 like and unlike update the visible like state', async ({ page }) => {
    await signIn(page);
    await page.goto(`/u/${users.alice.username}`);

    const card = postCardByText(page, seedData.aliceVisiblePost);
    await expect(card).toBeVisible();

    const likeButton = card.locator('[data-testid^="post-like-button-"]');
    const likeCount = card.locator('[data-testid^="post-like-count-"]');
    const initialCountText = (await likeCount.textContent())?.trim() ?? '0';

    await likeButton.click();
    await expect(likeCount).not.toHaveText(initialCountText);

    await likeButton.click();
    await expect(likeCount).toHaveText(initialCountText);
  });

  test('UF-11 missing direct post shows an unavailable state', async ({ page }) => {
    await signIn(page);
    await page.goto('/posts/999999');

    await expect(page.getByTestId('post-unavailable-state')).toBeVisible();
    await page.getByTestId('post-unavailable-home-link').click();
    await expect(page).toHaveURL(/\/$/);
  });

  test('UF-12 sign out blocks access to protected routes until sign-in', async ({ page }) => {
    await signIn(page);

    await page.getByTestId('nav-signout-button').click();
    await expect(page).toHaveURL(/\/signin$/);

    await page.goto('/');
    await expect(page).toHaveURL(/\/signin$/);
    await expect(page.getByTestId('signin-redirect-message')).toBeVisible();
  });
});
