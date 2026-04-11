import type { PostSummary, UserProfile } from '../api/contracts'

export function createMockPost(overrides?: Partial<PostSummary>): PostSummary {
  return {
    id: 1,
    authorUsername: 'alice',
    authorDisplayName: 'Alice Example',
    body: 'Test post content',
    createdAtUtc: new Date().toISOString(),
    isEdited: false,
    likeCount: 0,
    likedByViewer: false,
    canEdit: false,
    canDelete: false,
    ...overrides,
  }
}

export function createMockProfile(overrides?: Partial<UserProfile>): UserProfile {
  return {
    username: 'alice',
    displayName: 'Alice Example',
    bio: 'Test bio',
    followerCount: 0,
    followingCount: 0,
    isSelf: false,
    isFollowedByViewer: false,
    ...overrides,
  }
}
