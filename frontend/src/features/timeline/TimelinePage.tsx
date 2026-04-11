import { useState, useEffect } from 'react'
import { Composer } from '../posts/composer/Composer'
import { PostEditor } from '../posts/editor/PostEditor'
import { PostCard } from '../posts/post-card/PostCard'
import { ConfirmDialog } from '../../shared/components/ConfirmDialog'
import { apiClient } from '../../shared/api/client'
import type {
  PostInteractionState,
  PostSummary,
  TimelineResponse,
} from '../../shared/api/contracts'

export function TimelinePage() {
  const [posts, setPosts] = useState<PostSummary[]>([])
  const [nextCursor, setNextCursor] = useState<string | null>(null)
  const [editingPostId, setEditingPostId] = useState<number | null>(null)
  const [deletingPostId, setDeletingPostId] = useState<number | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isLoadingMore, setIsLoadingMore] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)
  const [pendingLikePostId, setPendingLikePostId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    loadTimeline()
  }, [])

  async function loadTimeline() {
    setIsLoading(true)
    setError(null)

    try {
      const data = await apiClient.get<TimelineResponse>('/timeline')

      setPosts(data.posts)
      setNextCursor(data.nextCursor ?? null)
    } catch (err) {
      setError('Failed to load timeline. Please try again.')
    } finally {
      setIsLoading(false)
    }
  }

  async function loadMorePosts() {
    if (!nextCursor || isLoadingMore) return

    setIsLoadingMore(true)

    try {
      const data = await apiClient.get<TimelineResponse>(`/timeline?cursor=${nextCursor}`)

      setPosts(prev => [...prev, ...data.posts])
      setNextCursor(data.nextCursor ?? null)
    } catch (err) {
      setError('Failed to load more posts')
    } finally {
      setIsLoadingMore(false)
    }
  }

  async function handlePostCreated() {
    // Reload timeline to show new post
    await loadTimeline()
  }

  function updatePost(postId: number, updater: (post: PostSummary) => PostSummary) {
    setPosts((currentPosts) =>
      currentPosts.map((post) => (post.id === postId ? updater(post) : post))
    )
  }

  async function handleEdit(postId: number, newBody: string) {
    await apiClient.patch(`/posts/${postId}`, { body: newBody })
    setEditingPostId(null)
    updatePost(postId, (post) => ({ ...post, body: newBody, isEdited: true }))
  }

  async function handleDelete(postId: number) {
    setIsDeleting(true)
    try {
      await apiClient.delete(`/posts/${postId}`)
      setDeletingPostId(null)
      setPosts((currentPosts) => currentPosts.filter((post) => post.id !== postId))
    } finally {
      setIsDeleting(false)
    }
  }

  async function handleLikeToggle(post: PostSummary) {
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

  if (isLoading) {
    return (
      <div className="max-w-2xl mx-auto p-4">
        <div className="text-center py-8">Loading timeline...</div>
      </div>
    )
  }

  return (
    <div className="max-w-2xl mx-auto p-4">
      <h1 className="text-3xl font-bold mb-6">Timeline</h1>

      <Composer onPostCreated={handlePostCreated} />

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 my-4">
          <p className="text-red-800">{error}</p>
          <button
            onClick={loadTimeline}
            className="mt-2 px-4 py-2 bg-red-100 hover:bg-red-200 rounded text-red-800"
          >
            Retry
          </button>
        </div>
      )}

      <div className="space-y-4 mt-6" data-testid="timeline-feed">
        {posts.length === 0 ? (
          <div className="bg-white rounded-lg shadow p-8 text-center text-gray-600">
            <p className="mb-2">Your timeline is empty.</p>
            <p className="text-sm">Create a post or follow other users to see content here.</p>
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
