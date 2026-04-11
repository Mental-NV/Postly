import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { apiClient } from '../../shared/api/client'
import type {
  PostInteractionState,
  PostSummary,
} from '../../shared/api/contracts'
import { isApiError } from '../../shared/api/errors'
import { PostEditor } from './editor/PostEditor'
import { PostCard } from './post-card/PostCard'
import { ConfirmDialog } from '../../shared/components/ConfirmDialog'
import { Button } from '../../shared/components/Button'

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
      void navigate('/')
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
        ? await apiClient.delete<PostInteractionState>(
            `/posts/${String(currentPost.id)}/like`
          )
        : await apiClient.post<PostInteractionState>(
            `/posts/${String(currentPost.id)}/like`
          )

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
      <div className="page-loading" data-testid="post-page">
        <div className="text-center py-8" data-testid="post-status">
          Loading post...
        </div>
      </div>
    )
  }

  if (notFound) {
    return (
      <div className="page-unavailable-state" data-testid="post-page">
        <div data-testid="post-unavailable-state">
          <h2 className="empty-title">Post not available</h2>
          <p className="empty-text">
            This post may have been deleted or does not exist.
          </p>
          <div style={{ marginTop: '24px' }}>
            <Button
              variant="primary"
              onClick={() => {
                void navigate('/')
              }}
              data-testid="post-unavailable-home-link"
            >
              Back to Home
            </Button>
          </div>
        </div>
      </div>
    )
  }

  if (error && !post) {
    return (
      <div className="page-error-container" data-testid="post-page">
        <p className="page-error-text" data-testid="post-status">
          {error}
        </p>
        <div className="error-actions">
          <Button
            variant="primary"
            onClick={() => {
              void loadPost()
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

  if (!post) return null

  return (
    <div className="post-detail-page" data-testid="post-page">
      <header className="page-header">
        <Button
          variant="ghost"
          onClick={() => {
            navigate(-1)
          }}
          className="back-btn"
          data-testid="post-back-link"
        >
          ←
        </Button>
        <h1 className="page-title">Post</h1>
      </header>

      {error && (
        <div className="page-error-container" style={{ padding: '16px' }}>
          <p className="page-error-text" data-testid="post-status">
            {error}
          </p>
        </div>
      )}

      <div className="post-detail-content">
        {editingPostId === post.id ? (
          <PostEditor
            post={post}
            onSave={(body) => handleEdit(post.id, body)}
            onCancel={() => {
              setEditingPostId(null)
            }}
          />
        ) : (
          <PostCard
            post={post}
            isLikePending={isLikePending}
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
