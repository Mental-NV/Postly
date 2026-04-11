import { useEffect, useState } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { apiClient } from '../../shared/api/client'
import type { PostInteractionState, PostSummary } from '../../shared/api/contracts'
import { isApiError } from '../../shared/api/errors'
import { PostEditor } from './editor/PostEditor'
import { PostCard } from './post-card/PostCard'
import { ConfirmDialog } from '../../shared/components/ConfirmDialog'

export function DirectPostPage() {
  const { postId } = useParams<{ postId: string }>()
  const navigate = useNavigate()

  const [post, setPost] = useState<PostSummary | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [notFound, setNotFound] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [editingPostId, setEditingPostId] = useState<number | null>(null)
  const [deletingPostId, setDeletingPostId] = useState<number | null>(null)
  const [isDeleting, setIsDeleting] = useState(false)
  const [isLikePending, setIsLikePending] = useState(false)

  useEffect(() => {
    void loadPost()
    // `loadPost` intentionally re-runs only when the route param changes.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [postId])

  const loadPost = async () => {
    if (!postId) return

    setIsLoading(true)
    setError(null)
    setNotFound(false)

    try {
      const data = await apiClient.get<PostSummary>(`/posts/${String(postId)}`)
      setPost(data)
    } catch (err: unknown) {
      if (isApiError(err) && err.status === 404) {
        setNotFound(true)
      } else {
        setError('Failed to load post. Please try again.')
      }
    } finally {
      setIsLoading(false)
    }
  }

  const handleEdit = async (postId: number, newBody: string) => {
    await apiClient.patch(`/posts/${String(postId)}`, { body: newBody })
    setEditingPostId(null)
    if (post) {
      setPost({ ...post, body: newBody, isEdited: true })
    }
  }

  const handleDelete = async (postId: number) => {
    setIsDeleting(true)
    try {
      await apiClient.delete(`/posts/${String(postId)}`)
      navigate('/')
    } finally {
      setIsDeleting(false)
    }
  }

  const handleLikeToggle = async (currentPost: PostSummary) => {
    if (isLikePending) return

    setIsLikePending(true)
    setError(null)

    const optimisticLikedByViewer = !currentPost.likedByViewer
    const optimisticLikeCount = Math.max(
      0,
      currentPost.likeCount + (optimisticLikedByViewer ? 1 : -1)
    )

    setPost({
      ...currentPost,
      likedByViewer: optimisticLikedByViewer,
      likeCount: optimisticLikeCount,
    })

    try {
      const interactionState = currentPost.likedByViewer
        ? await apiClient.delete<PostInteractionState>(`/posts/${String(currentPost.id)}/like`)
        : await apiClient.post<PostInteractionState>(`/posts/${String(currentPost.id)}/like`)

      setPost((existingPost) =>
        existingPost == null
          ? existingPost
          : {
              ...existingPost,
              likedByViewer: interactionState.likedByViewer,
              likeCount: interactionState.likeCount,
            }
      )
    } catch {
      setPost(currentPost)
      setError(
        currentPost.likedByViewer
          ? 'Failed to unlike post. Please try again.'
          : 'Failed to like post. Please try again.'
      )
    } finally {
      setIsLikePending(false)
    }
  }

  if (isLoading) {
    return (
      <div className="max-w-2xl mx-auto p-4" data-testid="post-page">
        <div className="text-center py-8" data-testid="post-status">Loading post...</div>
      </div>
    )
  }

  if (notFound) {
    return (
      <div className="max-w-2xl mx-auto p-4" data-testid="post-page">
        <div
          className="bg-yellow-50 border border-yellow-200 rounded-lg p-8 text-center"
          data-testid="post-unavailable-state"
        >
          <h2 className="text-xl font-bold mb-2">Post not available</h2>
          <p className="text-gray-600 mb-4">This post may have been deleted or does not exist.</p>
          <Link
            to="/"
            data-testid="post-unavailable-home-link"
            className="inline-block px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
          >
            Back to Timeline
          </Link>
        </div>
      </div>
    )
  }

  if (error && !post) {
    return (
      <div className="max-w-2xl mx-auto p-4" data-testid="post-page">
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-4">
          <p className="text-red-800" data-testid="post-status">{error}</p>
        </div>
        <button
          onClick={() => {
            void loadPost()
          }}
          className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
        >
          Retry
        </button>
        <Link to="/" className="ml-2 px-4 py-2 bg-gray-200 rounded hover:bg-gray-300">
          Back to Timeline
        </Link>
      </div>
    )
  }

  if (!post) return null

  return (
    <div className="max-w-2xl mx-auto p-4" data-testid="post-page">
      <Link
        to="/"
        data-testid="post-back-link"
        className="inline-flex items-center text-blue-500 hover:underline mb-4"
      >
        ← Back to Timeline
      </Link>

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-4">
          <p className="text-red-800" data-testid="post-status">{error}</p>
        </div>
      )}

      {editingPostId === post.id ? (
        <PostEditor
          post={post}
          onSave={(body) => handleEdit(post.id, body)}
          onCancel={() => setEditingPostId(null)}
        />
      ) : (
        <PostCard
          post={post}
          isLikePending={isLikePending}
          onLikeToggle={handleLikeToggle}
          onEdit={(currentPost) => setEditingPostId(currentPost.id)}
          onDelete={(currentPost) => setDeletingPostId(currentPost.id)}
        />
      )}

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
          onCancel={() => setDeletingPostId(null)}
          isPending={isDeleting}
        />
    </div>
  )
}
