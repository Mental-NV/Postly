import { useEffect, useState } from 'react'
import { Navigate, useLocation, useParams, useNavigate } from 'react-router-dom'
import { apiClient } from '../../shared/api/client'
import type {
  PostInteractionState,
  PostSummary,
  ProfileResponse,
  UserProfile,
} from '../../shared/api/contracts'
import { PostCard } from '../posts/post-card/PostCard'
import { PostEditor } from '../posts/editor/PostEditor'
import { ConfirmDialog } from '../../shared/components/ConfirmDialog'
import { Avatar } from '../../shared/components/Avatar'
import { Button } from '../../shared/components/Button'
import { useAuth } from '../../app/providers/AuthProvider'

export function ProfilePage(): React.JSX.Element {
  const { username } = useParams<{ username: string }>()
  const navigate = useNavigate()
  const location = useLocation()
  const { isAuthenticated, isLoading: isAuthLoading } = useAuth()

  const [profile, setProfile] = useState<UserProfile | null>(null)
  const [posts, setPosts] = useState<PostSummary[]>([])
  const [nextCursor, setNextCursor] = useState<string | null>(null)
  const [editingPostId, setEditingPostId] = useState<number | null>(null)
  const [deletingPostId, setDeletingPostId] = useState<number | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isLoadingMore, setIsLoadingMore] = useState(false)
  const [isFollowPending, setIsFollowPending] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)
  const [pendingLikePostId, setPendingLikePostId] = useState<number | null>(
    null
  )
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!username) return
    if (username === 'me' && !isAuthenticated) {
      return
    }

    void loadProfile()
  }, [isAuthenticated, username])

  useEffect(() => {
    if (!profile?.isSelf || username !== 'me') {
      return
    }

    void navigate(`/u/${profile.username}`, { replace: true })
  }, [navigate, profile, username])

  const loadProfile = async (): Promise<void> => {
    if (!username) return

    setIsLoading(true)
    setError(null)

    const apiPath =
      username === 'me' ? '/profiles/me' : `/profiles/${username}`

    try {
      const data = await apiClient.get<ProfileResponse>(apiPath)

      setProfile(data.profile)
      setPosts(data.posts)
      setNextCursor(data.nextCursor ?? null)
    } catch (err: unknown) {
      if (err && typeof err === 'object' && 'status' in err && err.status === 404) {
        setError('User not found')
      } else {
        setError('Failed to load profile. Please try again.')
      }
    } finally {
      setIsLoading(false)
    }
  }

  const loadMorePosts = async (): Promise<void> => {
    if (!username || !nextCursor || isLoadingMore) return

    setIsLoadingMore(true)

    const apiPath =
      username === 'me' ? '/profiles/me' : `/profiles/${username}`

    try {
      const data = await apiClient.get<ProfileResponse>(
        `${apiPath}?cursor=${nextCursor}`
      )

      setPosts((currentPosts) => [...currentPosts, ...data.posts])
      setNextCursor(data.nextCursor ?? null)
    } catch {
      setError('Failed to load more posts')
    } finally {
      setIsLoadingMore(false)
    }
  }

  const handleFollow = async (): Promise<void> => {
    if (!username || !profile || isFollowPending) return

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
    if (!username || !profile || isFollowPending) return

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
    if (pendingLikePostId === post.id) return

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
        ? await apiClient.delete<PostInteractionState>(
            `/posts/${post.id}/like`
          )
        : await apiClient.post<PostInteractionState>(
            `/posts/${post.id}/like`
          )

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

  if (!profile) return <div>Loading...</div>

  return (
    <div className="profile-page" data-testid="profile-page">
      <header className="page-header">
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
          <h1 className="page-title" data-testid="profile-display-name">{profile.displayName}</h1>
          <span className="header-post-count">{posts.length} Posts</span>
        </div>
      </header>

      <div className="profile-hero">
        <div className="profile-banner" />
        <div className="profile-avatar-row">
          <Avatar
            username={profile.username}
            displayName={profile.displayName}
            size="lg"
            className="profile-avatar-large"
          />
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
            ) : isAuthenticated && profile.isSelf ? (
              <Button variant="secondary" disabled data-testid="edit-profile-button">
                Edit Profile
              </Button>
            ) : null}
          </div>
        </div>

        <div className="profile-info-block">
          <div className="profile-names">
            <h2 className="profile-display-name">{profile.displayName}</h2>
            <span className="profile-username">@{profile.username}</span>
          </div>

          {profile.bio ? <p className="profile-bio">{profile.bio}</p> : null}

          <div className="profile-stats">
            <span className="stat-item">
              <strong className="stat-value">{profile.followingCount}</strong>{' '}
              Following
            </span>
            <span className="stat-item">
              <strong className="stat-value">{profile.followerCount}</strong>{' '}
              Followers
            </span>
          </div>
        </div>

        <nav className="profile-tabs">
          <div className="tab active">Posts</div>
        </nav>
      </div>

      {error ? <div className="page-error-container" style={{ padding: '16px' }}>
          <p className="page-error-text" role="alert">
            {error}
          </p>
        </div> : null}

      <div className="profile-posts-feed" data-testid="profile-posts-feed">
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
                  onCancel={() => { setEditingPostId(null); }}
                />
              ) : (
                <PostCard
                  key={post.id}
                  post={post}
                  isLikePending={pendingLikePostId === post.id}
                  showLikeButton={isAuthenticated}
                  onLikeToggle={(p) => {
                    void handleLikeToggle(p)
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

            {nextCursor ? <div className="load-more-container">
                <Button
                  variant="secondary"
                  onClick={() => {
                    void loadMorePosts()
                  }}
                  disabled={isLoadingMore}
                  className="load-more-btn"
                >
                  {isLoadingMore ? 'Loading...' : 'Load more'}
                </Button>
              </div> : null}
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
