import type { PostSummary, UserProfile } from './api/contracts'

export interface ProfileIdentityUpdate {
  username: string
  displayName: string
  avatarUrl: string | null
}

const PROFILE_IDENTITY_UPDATED_EVENT = 'postly:profile-identity-updated'

export function emitProfileIdentityUpdated(profile: UserProfile): void {
  window.dispatchEvent(
    new CustomEvent<ProfileIdentityUpdate>(PROFILE_IDENTITY_UPDATED_EVENT, {
      detail: {
        username: profile.username,
        displayName: profile.displayName,
        avatarUrl: profile.avatarUrl ?? null,
      },
    })
  )
}

export function subscribeToProfileIdentityUpdates(
  callback: (update: ProfileIdentityUpdate) => void
): () => void {
  const handler = (event: Event): void => {
    const customEvent = event as CustomEvent<ProfileIdentityUpdate>
    callback(customEvent.detail)
  }

  window.addEventListener(PROFILE_IDENTITY_UPDATED_EVENT, handler)

  return () => {
    window.removeEventListener(PROFILE_IDENTITY_UPDATED_EVENT, handler)
  }
}

export function applyProfileIdentityUpdateToPost(
  post: PostSummary,
  update: ProfileIdentityUpdate
): PostSummary {
  if (post.authorUsername !== update.username) {
    return post
  }

  return {
    ...post,
    authorDisplayName: update.displayName,
    authorAvatarUrl: update.avatarUrl,
  }
}
