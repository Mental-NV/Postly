import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
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

export function ProfilePage() {
  const { username } = useParams<{ username: string }>()

  const [profile, setProfile] = useState<UserProfile | null>(null)
  const [posts, setPosts] = useState<PostSummary[]>([])
  const [nextCursor, setNextCursor] = useState<string | null>(null)
  const [editingPostId, setEditingPostId] = useState<number | null>(null)
  const [deletingPostId, setDeletingPostId] = useState<number | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isLoadingMore, setIsLoadingMore] = useState(false)
  const [isFollowPending, setIsFollowPending] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)
  const [pendingLikePostId, setPendingLikePostId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    loadProfile()
  }, [username])

  const loadProfile = async () => {
    if (!username) return

    setIsLoading(true)
    setError(null)

    try {
      const data = await apiClient.get<ProfileResponse>(`/profiles/${username}`)

      setProfile(data.profile)
      setPosts(data.posts)
      setNextCursor(data.nextCursor ?? null)
    } catch (err: any) {
      if (err?.status === 404) {
        setError('User not found')
      } else {
        setError('Failed to load profile. Please try again.')
      }
    } finally {
      setIsLoading(false)
    }
  }

  const loadMorePosts = async () => {
    if (!username || !nextCursor || isLoadingMore) return

    setIsLoadingMore(true)

    try {
      const data = await apiClient.get<ProfileResponse>(`/profiles/${username}?cursor=${nextCursor}`)

      setPosts((currentPosts) => [...currentPosts, ...data.posts])
      setNextCursor(data.nextCursor ?? null)
    } catch (err) {
      setError('Failed to load more posts')
    } finally {
      setIsLoadingMore(false)
    }
  }

  const handleFollow = async () => {
    if (!username || !profile || isFollowPending) return

    setIsFollowPending(true)
    const wasFollowing = profile.isFollowedByViewer

    setProfile({
      ...profile,
      isFollowedByViewer: true,
      followerCount: profile.followerCount + 1,
    })

    try {
      await apiClient.post(`/profiles/${username}/follow`)
    } catch (err) {
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

  const handleUnfollow = async () => {
    if (!username || !profile || isFollowPending) return

    setIsFollowPending(true)
    const wasFollowing = profile.isFollowedByViewer

    setProfile({
      ...profile,
      isFollowedByViewer: false,
      followerCount: profile.followerCount - 1,
    })

    try {
      await apiClient.delete(`/profiles/${username}/follow`)
    } catch (err) {
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

  function updatePost(postId: number, updater: (post: PostSummary) => PostSummary) {
    setPosts((currentPosts) =>
      currentPosts.map((post) => (post.id === postId ? updater(post) : post))
    )
  }

  const handleEdit = async (postId: number, newBody: string) => {
    await apiClient.patch(`/posts/${postId}`, { body: newBody })
    setEditingPostId(null)
    updatePost(postId, (post) => ({ ...post, body: newBody, isEdited: true }))
  }

  const handleDelete = async (postId: number) => {
    setIsDeleting(true)

    try {
      await apiClient.delete(`/posts/${postId}`)
      setDeletingPostId(null)
      setPosts((currentPosts) => currentPosts.filter((post) => post.id !== postId))
    } finally {
      setIsDeleting(false)
    }
  }

  const handleLikeToggle = async (post: PostSummary) => {
    if (pendingLikePostId === post.id) return

    setPendingLikePostId(post.id)
    setError(null)

    const optimisticLikedByViewer = !post.likedByViewer
    const optimisticLikeCount = Math.max(0, post.likeCount + (optimisticLikedByViewer ? 1 : -1))

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
    } catch (err) {
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

  const getInitials = (displayName: string) => {
    return displayName
      .split(' ')
      .map((word) => word[0])
      .join('')
      .toUpperCase()
      .slice(0, 2)
  }

  if (isLoading) {
    return (
      <div className="max-w-2xl mx-auto p-4">
        <div className="text-center py-8">Loading profile...</div>
      </div>
    );
  }

  if (error && !profile) {
    return (
      <div className="max-w-2xl mx-auto p-4">
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-4">
          <p className="text-red-800">{error}</p>
        </div>
        <button
          onClick={loadProfile}
          className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
        >
          Retry
        </button>
        <Link to="/" className="ml-2 px-4 py-2 bg-gray-200 rounded hover:bg-gray-300">
          Back to Timeline
        </Link>
      </div>
    );
  }

  if (!profile) return null

  return (
    <div className="max-w-2xl mx-auto p-4">
      {/* Profile Header */}
      <div className="bg-white rounded-lg shadow p-6 mb-6">
        <div className="flex items-start justify-between">
          <div className="flex items-start space-x-4">
            {/* Avatar */}
            <div className="w-20 h-20 rounded-full bg-gradient-to-br from-blue-400 to-purple-500 flex items-center justify-center text-white text-2xl font-bold">
              {getInitials(profile.displayName)}
            </div>

            {/* Profile Info */}
            <div>
              <h1 className="text-2xl font-bold">{profile.displayName}</h1>
              <p className="text-gray-600">@{profile.username}</p>
              {profile.bio && (
                <p className="mt-2 text-gray-800">{profile.bio}</p>
              )}
              <div className="flex space-x-4 mt-3 text-sm">
                <span>
                  <strong>{profile.followingCount}</strong> Following
                </span>
                <span>
                  <strong>{profile.followerCount}</strong> Followers
                </span>
              </div>
            </div>
          </div>

          {/* Follow Button */}
          {!profile.isSelf && (
            <button
              onClick={profile.isFollowedByViewer ? handleUnfollow : handleFollow}
              disabled={isFollowPending}
              className={`px-4 py-2 rounded font-medium ${
                profile.isFollowedByViewer
                  ? 'bg-gray-200 hover:bg-gray-300 text-gray-800'
                  : 'bg-blue-500 hover:bg-blue-600 text-white'
              } disabled:opacity-50 disabled:cursor-not-allowed`}
            >
              {isFollowPending ? 'Loading...' : profile.isFollowedByViewer ? 'Unfollow' : 'Follow'}
            </button>
          )}

          {profile.isSelf && (
            <span className="px-4 py-2 bg-blue-50 text-blue-700 rounded font-medium">
              Your profile
            </span>
          )}
        </div>
      </div>

      {/* Error Message */}
      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-4">
          <p className="text-red-800">{error}</p>
        </div>
      )}

      {/* Posts Section */}
      <div className="space-y-4" data-testid="profile-posts-feed">
        <h2 className="text-xl font-bold">Posts</h2>

        {posts.length === 0 ? (
          <div className="bg-white rounded-lg shadow p-8 text-center text-gray-600">
            {profile.isSelf ? (
              <>
                <p>You haven't posted yet.</p>
                <Link to="/" className="text-blue-500 hover:underline mt-2 inline-block">
                  Create your first post!
                </Link>
              </>
            ) : (
              <p>{profile.displayName} hasn't posted yet.</p>
            )}
          </div>
        ) : (
          <>
            {posts.map((post) =>
              editingPostId === post.id ? (
                <PostEditor
                  key={post.id}
                  post={post}
                  onSave={(body) => handleEdit(post.id, body)}
                  onCancel={() => setEditingPostId(null)}
                />
              ) : (
                <PostCard
                  key={post.id}
                  post={post}
                  isLikePending={pendingLikePostId === post.id}
                  onLikeToggle={handleLikeToggle}
                  onEdit={(currentPost) => setEditingPostId(currentPost.id)}
                  onDelete={(currentPost) => setDeletingPostId(currentPost.id)}
                />
              )
            )}

            {nextCursor && (
              <button
                onClick={loadMorePosts}
                disabled={isLoadingMore}
                className="w-full py-2 bg-gray-100 hover:bg-gray-200 rounded disabled:opacity-50"
              >
                {isLoadingMore ? 'Loading...' : 'Load more'}
              </button>
            )}
          </>
        )}
      </div>

      <ConfirmDialog
        isOpen={deletingPostId !== null}
        title="Delete Post"
        message="Are you sure you want to delete this post? This action cannot be undone."
        confirmText="Delete"
        onConfirm={() => deletingPostId && handleDelete(deletingPostId)}
        onCancel={() => setDeletingPostId(null)}
        isPending={isDeleting}
      />
    </div>
  )
}
