import type {
  PostSummary,
  ProfileResponse,
  UserProfile,
  ConversationResponse,
} from '../api/contracts'

export function createMockPost(overrides?: Partial<PostSummary>): PostSummary {
  return {
    id: 1,
    authorUsername: 'alice',
    authorDisplayName: 'Alice Example',
    authorAvatarUrl: null,
    body: 'Test post content',
    createdAtUtc: new Date().toISOString(),
    isEdited: false,
    likeCount: 0,
    likedByViewer: false,
    canEdit: false,
    canDelete: false,
    isReply: false,
    replyToPostId: null,
    state: 'available',
    ...overrides,
  }
}

export function createMockConversation(
  overrides?: Partial<ConversationResponse>
): ConversationResponse {
  return {
    target: {
      state: 'available',
      post: createMockPost({ id: 10, authorUsername: 'alice', body: 'Conversation post' }),
    },
    replies: [],
    nextCursor: null,
    ...overrides,
  }
}

export function createMockProfile(
  overrides?: Partial<UserProfile>
): UserProfile {
  return {
    username: 'alice',
    displayName: 'Alice Example',
    bio: 'Test bio',
    avatarUrl: null,
    hasCustomAvatar: false,
    followerCount: 0,
    followingCount: 0,
    isSelf: false,
    isFollowedByViewer: false,
    ...overrides,
  }
}

export function createVersionedAvatarUrl(
  username: string,
  version: number | string
): string {
  return `/api/profiles/${username}/avatar?v=${version}`
}

export function createMockBobProfileEditFixture(
  overrides?: {
    profile?: Partial<UserProfile>
    post?: Partial<PostSummary>
  }
): ProfileResponse {
  const avatarUrl =
    overrides?.profile?.avatarUrl === undefined
      ? null
      : overrides.profile.avatarUrl

  return {
    profile: createMockProfile({
      username: 'bob',
      displayName: 'Bob Tester',
      bio: 'Primary seeded user for Postly e2e flows.',
      avatarUrl,
      hasCustomAvatar: avatarUrl != null,
      isSelf: true,
      ...overrides?.profile,
    }),
    posts: [
      createMockPost({
        id: 2,
        authorUsername: 'bob',
        authorDisplayName: 'Bob Tester',
        authorAvatarUrl: avatarUrl,
        body: 'Seed post from Bob',
        canEdit: true,
        canDelete: true,
        ...overrides?.post,
      }),
    ],
    nextCursor: undefined,
  }
}

export function createMockBobAvatarFallbackFixture(): ProfileResponse {
  return createMockBobProfileEditFixture({
    profile: {
      avatarUrl: null,
      hasCustomAvatar: false,
    },
    post: {
      authorAvatarUrl: null,
    },
  })
}
