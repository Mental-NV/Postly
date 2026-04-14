import { useState, useEffect } from 'react'
import { Composer } from '../posts/composer/Composer'
import { PostEditor } from '../posts/editor/PostEditor'
import { PostCard } from '../posts/post-card/PostCard'
import { ConfirmDialog } from '../../shared/components/ConfirmDialog'
import { Button } from '../../shared/components/Button'
import { apiClient } from '../../shared/api/client'
import type {
  PostInteractionState,
  PostSummary,
  TimelineResponse,
} from '../../shared/api/contracts'

export function TimelinePage(): React.JSX.Element {
  const [posts, setPosts] = useState<PostSummary[]>([])
  const [nextCursor, setNextCursor] = useState<string | null>(null)
  const [editingPostId, setEditingPostId] = useState<number | null>(null)
  const [deletingPostId, setDeletingPostId] = useState<number | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isLoadingMore, setIsLoadingMore] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)
  const [pendingLikePostId, setPendingLikePostId] = useState<number | null>(
    null
  )
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    void loadTimeline()
  }, [])

  async function loadTimeline(): Promise<void> {
    setIsLoading(true)
    setError(null)

    try {
      const data = await apiClient.get<TimelineResponse>('/timeline')

      setPosts(data.posts)
      setNextCursor(data.nextCursor ?? null)
    } catch {
      setError('Failed to load timeline. Please try again.')
    } finally {
      setIsLoading(false)
    }
  }

  async function loadMorePosts(): Promise<void> {
    if (!nextCursor || isLoadingMore) return

    setIsLoadingMore(true)

    try {
      const data = await apiClient.get<TimelineResponse>(
        `/timeline?cursor=${nextCursor}`
      )

      setPosts((prev) => [...prev, ...data.posts])
      setNextCursor(data.nextCursor ?? null)
    } catch {
      setError('Failed to load more posts')
    } finally {
      setIsLoadingMore(false)
    }
  }

  async function handlePostCreated(): Promise<void> {
    // Reload timeline to show new post
    await loadTimeline()
  }

  function updatePost(
    postId: number,
    updater: (post: PostSummary) => PostSummary
  ): void {
    setPosts((currentPosts) =>
      currentPosts.map((post) => (post.id === postId ? updater(post) : post))
    )
  }

  async function handleEdit(postId: number, newBody: string): Promise<void> {
    await apiClient.patch(`/posts/${String(postId)}`, { body: newBody })
    setEditingPostId(null)
    updatePost(postId, (post) => ({ ...post, body: newBody, isEdited: true }))
  }

  async function handleDelete(postId: number): Promise<void> {
    setIsDeleting(true)
    try {
      await apiClient.delete(`/posts/${String(postId)}`)
      setDeletingPostId(null)
      setPosts((currentPosts) =>
        currentPosts.filter((post) => post.id !== postId)
      )
    } finally {
      setIsDeleting(false)
    }
  }

  async function handleLikeToggle(post: PostSummary): Promise<void> {
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
            `/posts/${String(post.id)}/like`
          )
        : await apiClient.post<PostInteractionState>(
            `/posts/${String(post.id)}/like`
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

  if (isLoading) {
    return (
      <div className="page-loading">
        <div className="text-center py-8">Loading timeline...</div>
      </div>
    )
  }

  return (
    <div className="timeline-page">
      <header className="page-header">
        <h1 className="page-title">Home</h1>
      </header>

      <Composer
        onPostCreated={() => {
          void handlePostCreated()
        }}
      />

      {error ? <div className="page-error-container">
          <p className="page-error-text">{error}</p>
          <Button
            variant="secondary"
            onClick={() => {
              void loadTimeline()
            }}
          >
            Retry
          </Button>
        </div> : null}

      <div className="timeline-feed" data-testid="timeline-feed">
        {posts.length === 0 ? (
          <div className="page-empty-state">
            <h2 className="empty-title">Welcome to Postly!</h2>
            <p className="empty-text">
              Your timeline is empty. Create a post or follow other users to see
              content here.
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
