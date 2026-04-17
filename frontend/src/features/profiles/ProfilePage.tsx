import { useCallback, useEffect, useState } from 'react'
import { Navigate, useLocation, useNavigate, useParams } from 'react-router-dom'
import {
  apiClient,
  getProfilePath,
} from '../../shared/api/client'
import type {
  PostInteractionState,
  PostSummary,
  ProfileResponse,
  UpdateProfileRequest,
  UserProfile,
} from '../../shared/api/contracts'
import {
  getFieldErrors,
  getFormErrorMessage,
  isApiError,
} from '../../shared/api/errors'
import {
  applyProfileIdentityUpdateToPost,
  emitProfileIdentityUpdated,
} from '../../shared/profileIdentityEvents'
import { Avatar } from '../../shared/components/Avatar'
import { Button } from '../../shared/components/Button'
import { ConfirmDialog } from '../../shared/components/ConfirmDialog'
import {
  ContinuationEndState,
  ContinuationErrorState,
  ContinuationLoadingState,
} from '../../shared/components/LoadingState'
import { useAuth } from '../../app/providers/AuthContext'
import { PostEditor } from '../posts/editor/PostEditor'
import { PostCard } from '../posts/post-card/PostCard'
import { Camera } from 'lucide-react'
import { useContinuationCollection } from '../../shared/hooks/useContinuationCollection'

interface ProfileFormErrors {
  displayName?: string
  bio?: string
  avatar?: string
}

interface ProfileFormStatus {
  kind: 'pending' | 'success' | 'error'
  message: string
}

const MAX_AVATAR_UPLOAD_BYTES = 5 * 1024 * 1024

export function ProfilePage(): React.JSX.Element {
  const { username } = useParams<{ username: string }>()
  const navigate = useNavigate()
  const location = useLocation()
  const { isAuthenticated, isLoading: isAuthLoading } = useAuth()

  const [profile, setProfile] = useState<UserProfile | null>(null)
  const [editingPostId, setEditingPostId] = useState<number | null>(null)
  const [deletingPostId, setDeletingPostId] = useState<number | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isFollowPending, setIsFollowPending] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)
  const [pendingLikePostId, setPendingLikePostId] = useState<number | null>(
    null
  )
  const [error, setError] = useState<string | null>(null)
  const {
    items: posts,
    setItems: setPosts,
    reset,
    retry,
    sentinelRef,
    status: continuationStatus,
    errorMessage: continuationError,
    shouldRenderContinuation,
  } = useContinuationCollection<PostSummary>({
    getKey: (post) => post.id,
    loadMore: async (cursor) => {
      if (!username) {
        return {
          items: [],
          nextCursor: null,
        }
      }

      const data = await apiClient.get<ProfileResponse>(
        getProfilePath(username, cursor)
      )
      return {
        items: data.posts,
        nextCursor: data.nextCursor,
      }
    },
    loadMoreErrorMessage: 'Failed to load more posts. Please try again.',
  })

  const [isEditingProfile, setIsEditingProfile] = useState(false)
  const [isSavingProfile, setIsSavingProfile] = useState(false)
  const [displayNameDraft, setDisplayNameDraft] = useState('')
  const [bioDraft, setBioDraft] = useState('')
  const [avatarFile, setAvatarFile] = useState<File | null>(null)
  const [profileFormErrors, setProfileFormErrors] = useState<ProfileFormErrors>(
    {}
  )
  const [profileFormStatus, setProfileFormStatus] =
    useState<ProfileFormStatus | null>(null)

  const syncProfileDrafts = useCallback((currentProfile: UserProfile): void => {
    setDisplayNameDraft(currentProfile.displayName)
    setBioDraft(currentProfile.bio ?? '')
    setAvatarFile(null)
    setProfileFormErrors({})
  }, [])

  const loadProfile = useCallback(async (): Promise<void> => {
    if (!username) {
      return
    }

    setIsLoading(true)
    setError(null)

    try {
      const data = await apiClient.get<ProfileResponse>(getProfilePath(username))

      setProfile(data.profile)
      reset({
        items: data.posts,
        nextCursor: data.nextCursor,
      })
    } catch (err: unknown) {
      if (isApiError(err) && err.status === 404) {
        setError('User not found')
      } else {
        setError('Failed to load profile. Please try again.')
      }
    } finally {
      setIsLoading(false)
    }
  }, [reset, username])

  useEffect(() => {
    if (!username) {
      return
    }

    if (username === 'me' && !isAuthenticated) {
      return
    }

    void loadProfile()
  }, [isAuthenticated, loadProfile, username])

  useEffect(() => {
    if (!profile?.isSelf || username !== 'me') {
      return
    }

    void navigate(`/u/${profile.username}`, { replace: true })
  }, [navigate, profile, username])

  useEffect(() => {
    if (profile != null && !isEditingProfile) {
      syncProfileDrafts(profile)
    }
  }, [isEditingProfile, profile, syncProfileDrafts])

  const handleFollow = async (): Promise<void> => {
    if (!username || !profile || isFollowPending) {
      return
    }

    setIsFollowPending(true)
    setError(null)
    const wasFollowing = profile.isFollowedByViewer

    setProfile({
      ...profile,
      isFollowedByViewer: true,
      followerCount: profile.followerCount + 1,
    })

    try {
      await apiClient.post(`/profiles/${username}/follow`)
    } catch {
      setProfile({
        ...profile,
        isFollowedByViewer: wasFollowing,
        followerCount: profile.followerCount,
      })
      setError('Failed to follow user. Please try again.')
    } finally {
      setIsFollowPending(false)
    }
  }

  const handleUnfollow = async (): Promise<void> => {
    if (!username || !profile || isFollowPending) {
      return
    }

    setIsFollowPending(true)
    setError(null)
    const wasFollowing = profile.isFollowedByViewer

    setProfile({
      ...profile,
      isFollowedByViewer: false,
      followerCount: profile.followerCount - 1,
    })

    try {
      await apiClient.delete(`/profiles/${username}/follow`)
    } catch {
      setProfile({
        ...profile,
        isFollowedByViewer: wasFollowing,
        followerCount: profile.followerCount,
      })
      setError('Failed to unfollow user. Please try again.')
    } finally {
      setIsFollowPending(false)
    }
  }

  function updatePost(
    postId: number,
    updater: (post: PostSummary) => PostSummary
  ): void {
    setPosts((currentPosts) =>
      currentPosts.map((post) => (post.id === postId ? updater(post) : post))
    )
  }

  const handleEdit = async (postId: number, newBody: string): Promise<void> => {
    await apiClient.patch(`/posts/${postId}`, { body: newBody })
    setEditingPostId(null)
    updatePost(postId, (post) => ({ ...post, body: newBody, isEdited: true }))
  }

  const handleDelete = async (postId: number): Promise<void> => {
    setIsDeleting(true)

    try {
      await apiClient.delete(`/posts/${postId}`)
      setDeletingPostId(null)
      setPosts((currentPosts) =>
        currentPosts.filter((post) => post.id !== postId)
      )
    } finally {
      setIsDeleting(false)
    }
  }

  const handleLikeToggle = async (post: PostSummary): Promise<void> => {
    if (pendingLikePostId === post.id) {
      return
    }

    setPendingLikePostId(post.id)
    setError(null)

    const optimisticLikedByViewer = !post.likedByViewer
    const optimisticLikeCount = Math.max(
      0,
      post.likeCount + (optimisticLikedByViewer ? 1 : -1)
    )

    updatePost(post.id, (currentPost) => ({
      ...currentPost,
      likedByViewer: optimisticLikedByViewer,
      likeCount: optimisticLikeCount,
    }))

    try {
      const interactionState = post.likedByViewer
        ? await apiClient.delete<PostInteractionState>(`/posts/${post.id}/like`)
        : await apiClient.post<PostInteractionState>(`/posts/${post.id}/like`)

      updatePost(post.id, (currentPost) => ({
        ...currentPost,
        likedByViewer: interactionState.likedByViewer,
        likeCount: interactionState.likeCount,
      }))
    } catch {
      updatePost(post.id, (currentPost) => ({
        ...currentPost,
        likedByViewer: post.likedByViewer,
        likeCount: post.likeCount,
      }))
      setError(
        post.likedByViewer
          ? 'Failed to unlike post. Please try again.'
          : 'Failed to like post. Please try again.'
      )
    } finally {
      setPendingLikePostId(null)
    }
  }

  const validateProfileDrafts = (): ProfileFormErrors => {
    const errors: ProfileFormErrors = {}
    const trimmedDisplayName = displayNameDraft.trim()
    const trimmedBio = bioDraft.trim()

    if (trimmedDisplayName.length < 1 || trimmedDisplayName.length > 50) {
      errors.displayName =
        'Display name must be between 1 and 50 characters after trimming.'
    }

    if (trimmedBio.length > 160) {
      errors.bio = 'Bio must be 160 characters or fewer.'
    }

    if (avatarFile != null) {
      if (!['image/jpeg', 'image/png'].includes(avatarFile.type)) {
        errors.avatar = 'Avatar upload must be a still JPEG or PNG image.'
      } else if (avatarFile.size === 0) {
        errors.avatar = 'Avatar upload cannot be empty.'
      } else if (avatarFile.size > MAX_AVATAR_UPLOAD_BYTES) {
        errors.avatar = 'Avatar upload must be 5 MB or smaller.'
      }
    }

    return errors
  }

  const applyProfileIdentityToPosts = (
    currentPosts: PostSummary[],
    currentProfile: UserProfile
  ): PostSummary[] => {
    return currentPosts.map((post) =>
      applyProfileIdentityUpdateToPost(post, {
        username: currentProfile.username,
        displayName: currentProfile.displayName,
        avatarUrl: currentProfile.avatarUrl ?? null,
      })
    )
  }

  const handleProfileSave = async (): Promise<void> => {
    if (!profile || !profile.isSelf || isSavingProfile) {
      return
    }

    const clientErrors = validateProfileDrafts()
    setProfileFormErrors(clientErrors)

    if (Object.keys(clientErrors).length > 0) {
      setProfileFormStatus({
        kind: 'error',
        message: 'Resolve the highlighted profile fields and try again.',
      })
      return
    }

    const trimmedDisplayName = displayNameDraft.trim()
    const normalizedBio = bioDraft.trim()
    const originalProfile = profile
    const originalPosts = posts

    const profileUpdate: UpdateProfileRequest = {
      displayName: trimmedDisplayName,
      bio: normalizedBio.length === 0 ? null : normalizedBio,
    }

    const hasTextChanges =
      trimmedDisplayName !== originalProfile.displayName ||
      (normalizedBio.length === 0 ? null : normalizedBio) !==
        (originalProfile.bio ?? null)

    setIsSavingProfile(true)
    setProfileFormStatus({
      kind: 'pending',
      message: 'Saving profile…',
    })

    let textUpdatedOnServer = false
    let nextProfile = originalProfile

    try {
      if (hasTextChanges) {
        const profileResponse = await apiClient.patch<ProfileResponse>(
          '/profiles/me',
          profileUpdate
        )
        textUpdatedOnServer = true
        nextProfile = profileResponse.profile
      }

      if (avatarFile != null) {
        const formData = new FormData()
        formData.append('avatar', avatarFile)

        const avatarResponse = await apiClient.putForm<ProfileResponse>(
          '/profiles/me/avatar',
          formData
        )
        nextProfile = avatarResponse.profile
      }

      setProfile(nextProfile)
      setPosts((currentPosts) =>
        applyProfileIdentityToPosts(currentPosts, nextProfile)
      )
      emitProfileIdentityUpdated(nextProfile)
      setIsEditingProfile(false)
      setAvatarFile(null)
      setProfileFormErrors({})
      setProfileFormStatus({
        kind: 'success',
        message: 'Profile saved.',
      })
    } catch (saveError: unknown) {
      if (textUpdatedOnServer && avatarFile != null) {
        try {
          await apiClient.patch<ProfileResponse>('/profiles/me', {
            displayName: originalProfile.displayName,
            bio: originalProfile.bio ?? null,
          })
        } catch {
          setProfileFormStatus({
            kind: 'error',
            message:
              'Profile save failed after a partial update. Refresh to confirm the current saved profile.',
          })
          setIsSavingProfile(false)
          return
        }
      }

      setProfile(originalProfile)
      setPosts(originalPosts)
      setProfileFormErrors({
        displayName: getFieldErrors(saveError, 'displayName')[0],
        bio: getFieldErrors(saveError, 'bio')[0],
        avatar: getFieldErrors(saveError, 'avatar')[0],
      })
      setProfileFormStatus({
        kind: 'error',
        message:
          getFormErrorMessage(saveError) ?? 'Failed to save profile changes.',
      })
    } finally {
      setIsSavingProfile(false)
    }
  }

  const handleProfileCancel = (): void => {
    if (!profile) {
      return
    }

    syncProfileDrafts(profile)
    setProfileFormStatus(null)
    setIsEditingProfile(false)
  }

  const handleProfileEditStart = (): void => {
    if (!profile) {
      return
    }

    syncProfileDrafts(profile)
    setProfileFormStatus(null)
    setIsEditingProfile(true)
  }

  const handleAvatarInputChange = (
    event: React.ChangeEvent<HTMLInputElement>
  ): void => {
    const selectedFile = event.target.files?.[0] ?? null

    setAvatarFile(selectedFile)
    setProfileFormErrors((currentErrors) => ({
      ...currentErrors,
      avatar: undefined,
    }))
    setProfileFormStatus(null)
  }

  if (username === 'me') {
    if (isAuthLoading) {
      return (
        <div className="page-loading">
          <div className="text-center py-8">Loading profile...</div>
        </div>
      )
    }

    if (!isAuthenticated) {
      const returnUrl = location.pathname + location.search
      return (
        <Navigate
          to={`/signin?returnUrl=${encodeURIComponent(returnUrl)}`}
          replace
        />
      )
    }
  }

  if (isLoading) {
    return (
      <div className="page-loading">
        <div className="text-center py-8">Loading profile...</div>
      </div>
    )
  }

  if (error && !profile) {
    return (
      <div className="page-error-container">
        <p className="page-error-text">{error}</p>
        <div className="error-actions">
          <Button
            variant="primary"
            onClick={() => {
              void loadProfile()
            }}
          >
            Retry
          </Button>
          <Button
            variant="secondary"
            onClick={() => {
              void navigate('/')
            }}
          >
            Back to Home
          </Button>
        </div>
      </div>
    )
  }

  if (!profile) {
    return <div>Loading...</div>
  }

  const showFormStatus = profileFormStatus != null

  return (
    <div className="profile-page" data-testid="profile-page">
      <header className="page-header" data-testid="profile-heading">
        <Button
          variant="ghost"
          onClick={() => {
            void navigate(-1)
          }}
          className="back-btn"
        >
          ←
        </Button>
        <div className="header-info">
          <h1 className="page-title" data-testid="profile-display-name">
            {profile.displayName}
          </h1>
          <span className="header-post-count">{posts.length} Posts</span>
        </div>
        {profile.isSelf && isEditingProfile ? (
          <div className="profile-header-edit-actions">
            <Button
              variant="primary"
              onClick={() => {
                void handleProfileSave()
              }}
              disabled={isSavingProfile}
              data-testid="profile-save-button"
            >
              {isSavingProfile ? 'Saving…' : 'Save'}
            </Button>
            <Button
              variant="ghost"
              onClick={handleProfileCancel}
              disabled={isSavingProfile}
              data-testid="profile-cancel-button"
            >
              Cancel
            </Button>
          </div>
        ) : null}
      </header>

      <div className={`profile-hero ${isEditingProfile ? 'is-editing' : ''}`}>
        <div className="profile-banner" />
        <div className="profile-avatar-row">
          <div data-testid="profile-avatar-wrapper">
            <Avatar
              username={profile.username}
              displayName={profile.displayName}
              avatarUrl={profile.avatarUrl}
              size="lg"
              className="profile-avatar-large"
              imageTestId="profile-avatar-image"
              fallbackTestId="profile-avatar-fallback"
            />
            {profile.isSelf && isEditingProfile ? (
              <label
                className="profile-avatar-edit-overlay"
                data-testid="profile-avatar-edit-overlay"
              >
                <Camera size={16} aria-hidden="true" />
                <span>Change</span>
                <input
                  type="file"
                  accept="image/jpeg,image/png"
                  data-testid="profile-avatar-input"
                  onChange={handleAvatarInputChange}
                  style={{ display: 'none' }}
                />
              </label>
            ) : null}
          </div>

          <div className="profile-action-area">
            {isAuthenticated && !profile.isSelf ? (
              <Button
                variant={profile.isFollowedByViewer ? 'secondary' : 'primary'}
                onClick={() => {
                  void (profile.isFollowedByViewer
                    ? handleUnfollow()
                    : handleFollow())
                }}
                disabled={isFollowPending}
                data-testid="follow-unfollow-button"
              >
                {isFollowPending
                  ? '...'
                  : profile.isFollowedByViewer
                    ? 'Unfollow'
                    : 'Follow'}
              </Button>
            ) : null}

            {profile.isSelf ? (
              isEditingProfile ? null : (
                <Button
                  variant="secondary"
                  onClick={handleProfileEditStart}
                  data-testid="profile-edit-button"
                >
                  Edit Profile
                </Button>
              )
            ) : null}
          </div>
        </div>

        <div className="profile-info-block">
          <div className="profile-names">
            {isEditingProfile ? (
              <div className="profile-edit-form" data-testid="profile-edit-form">
                <label className="profile-edit-label">
                  <span className="profile-edit-label-text">Display name</span>
                  <input
                    type="text"
                    value={displayNameDraft}
                    onChange={(event) => {
                      setDisplayNameDraft(event.target.value)
                      setProfileFormErrors((currentErrors) => ({
                        ...currentErrors,
                        displayName: undefined,
                      }))
                    }}
                    disabled={isSavingProfile}
                    className="profile-edit-input profile-edit-input-display-name"
                    data-testid="profile-display-name-input"
                  />
                </label>
                {profileFormErrors.displayName ? (
                  <p role="alert" className="profile-edit-error">
                    {profileFormErrors.displayName}
                  </p>
                ) : null}

                <label className="profile-edit-label profile-bio-field">
                  <span className="profile-edit-label-text">Bio</span>
                  <textarea
                    value={bioDraft}
                    onChange={(event) => {
                      setBioDraft(event.target.value)
                      setProfileFormErrors((currentErrors) => ({
                        ...currentErrors,
                        bio: undefined,
                      }))
                    }}
                    disabled={isSavingProfile}
                    className="profile-edit-input profile-edit-textarea"
                    data-testid="profile-bio-input"
                  />
                  <div
                    className="profile-bio-counter"
                    data-testid="profile-bio-counter"
                  >
                    {bioDraft.trim().length}/160
                  </div>
                </label>
                {profileFormErrors.bio ? (
                  <p role="alert" className="profile-edit-error">
                    {profileFormErrors.bio}
                  </p>
                ) : null}
                {profileFormErrors.avatar ? (
                  <p role="alert" className="profile-edit-error">
                    {profileFormErrors.avatar}
                  </p>
                ) : null}
              </div>
            ) : (
              <>
                <h2 className="profile-display-name">{profile.displayName}</h2>
                <span className="profile-username">@{profile.username}</span>
              </>
            )}
          </div>

          {!isEditingProfile && profile.bio ? (
            <p className="profile-bio" data-testid="profile-bio">
              {profile.bio}
            </p>
          ) : null}

          <div
            className={`profile-stats ${isEditingProfile ? 'profile-stats-editing' : ''}`}
          >
            <span className="stat-item">
              <strong className="stat-value">{profile.followingCount}</strong>{' '}
              Following
            </span>
            <span className="stat-item">
              <strong className="stat-value">{profile.followerCount}</strong>{' '}
              Followers
            </span>
          </div>

          <div
            data-testid="profile-form-status"
            role={profileFormStatus?.kind === 'error' ? 'alert' : 'status'}
            aria-live="polite"
          >
            {showFormStatus ? profileFormStatus.message : ''}
          </div>
        </div>

        <nav className="profile-tabs">
          <div className="tab active">Posts</div>
        </nav>
      </div>

      {error ? (
        <div className="page-error-container" style={{ padding: '16px' }}>
          <p className="page-error-text" role="alert">
            {error}
          </p>
        </div>
      ) : null}

      <div className="profile-posts-feed" data-testid="profile-posts">
        {posts.length === 0 ? (
          <div className="page-empty-state">
            <h2 className="empty-title">
              {profile.isSelf ? "You haven't posted yet" : 'No posts yet'}
            </h2>
            <p className="empty-text">
              {profile.isSelf
                ? "When you post, they'll show up here."
                : `When @${profile.username} posts, they'll show up here.`}
            </p>
          </div>
        ) : (
          <>
            {posts.map((post) =>
              editingPostId === post.id ? (
                <PostEditor
                  key={post.id}
                  post={post}
                  onSave={(body) => handleEdit(post.id, body)}
                  onCancel={() => {
                    setEditingPostId(null)
                  }}
                />
              ) : (
                <PostCard
                  key={post.id}
                  post={post}
                  isLikePending={pendingLikePostId === post.id}
                  showLikeButton={isAuthenticated}
                  onLikeToggle={(currentPost) => {
                    void handleLikeToggle(currentPost)
                  }}
                  onEdit={(currentPost) => {
                    setEditingPostId(currentPost.id)
                  }}
                  onDelete={(currentPost) => {
                    setDeletingPostId(currentPost.id)
                  }}
                />
              )
            )}

            {shouldRenderContinuation ? (
              <>
                <div
                  data-testid="collection-continuation-sentinel"
                  ref={sentinelRef}
                  aria-hidden="true"
                />
                {continuationStatus === 'loading-more' ? (
                  <ContinuationLoadingState message="Loading more posts…" />
                ) : null}
                {continuationStatus === 'load-more-error' &&
                continuationError != null ? (
                  <ContinuationErrorState
                    message={continuationError}
                    onRetry={() => {
                      void retry()
                    }}
                  />
                ) : null}
                {continuationStatus === 'exhausted' ? (
                  <ContinuationEndState message="No more posts to show." />
                ) : null}
              </>
            ) : null}
          </>
        )}
      </div>

      <ConfirmDialog
        isOpen={deletingPostId !== null}
        title="Delete Post"
        message="Are you sure you want to delete this post? This action cannot be undone."
        confirmText="Delete"
        onConfirm={() => {
          if (deletingPostId !== null) {
            void handleDelete(deletingPostId)
          }
        }}
        onCancel={() => {
          setDeletingPostId(null)
        }}
        isPending={isDeleting}
      />
    </div>
  )
}
