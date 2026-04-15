import { useState, useCallback, useEffect, useRef } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { apiClient } from '../../shared/api/client'
import type {
  ConversationResponse,
  PostInteractionState,
  PostSummary,
  PostResponse,
} from '../../shared/api/contracts'
import { isApiError } from '../../shared/api/errors'
import { useAuth } from '../../app/providers/AuthContext'
import { PostEditor } from './editor/PostEditor'
import { PostCard } from './post-card/PostCard'
import { ConfirmDialog } from '../../shared/components/ConfirmDialog'
import { Button } from '../../shared/components/Button'
import {
  applyProfileIdentityUpdateToPost,
  subscribeToProfileIdentityUpdates,
} from '../../shared/profileIdentityEvents'

export function DirectPostPage(): React.JSX.Element | null {
  const { postId } = useParams<{ postId: string }>()
  const navigate = useNavigate()
  const { isAuthenticated } = useAuth()

  const [conversation, setConversation] = useState<ConversationResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [notFound, setNotFound] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [editingPostId, setEditingPostId] = useState<number | null>(null)
  const [deletingPostId, setDeletingPostId] = useState<number | null>(null)
  const [isDeleting, setIsDeleting] = useState(false)
  const [isLikePending, setIsLikePending] = useState(false)

  // Reply composer state
  const [replyBody, setReplyBody] = useState('')
  const [isReplyPending, setIsReplyPending] = useState(false)
  const [replyError, setReplyError] = useState<string | null>(null)
  const replyInputRef = useRef<HTMLTextAreaElement>(null)

  const loadConversation = useCallback(async (): Promise<void> => {
    if (!postId) return

    setIsLoading(true)
    setError(null)
    setNotFound(false)

    try {
      const data = await apiClient.get<ConversationResponse>(`/posts/${String(postId)}`)
      setConversation(data)
    } catch (err: unknown) {
      if (isApiError(err) && err.status === 404) {
        setNotFound(true)
      } else {
        setError('Failed to load post. Please try again.')
      }
    } finally {
      setIsLoading(false)
    }
  }, [postId])

  useEffect(() => {
    void loadConversation()
  }, [loadConversation])

  useEffect(() => {
    return subscribeToProfileIdentityUpdates((update) => {
      setConversation((current) => {
        if (current == null) return current
        const updatedTarget = current.target.post
          ? { ...current.target, post: applyProfileIdentityUpdateToPost(current.target.post, update) }
          : current.target
        const updatedReplies = current.replies.map((r) =>
          applyProfileIdentityUpdateToPost(r, update)
        )
        return { ...current, target: updatedTarget, replies: updatedReplies }
      })
    })
  }, [])

  const handleEdit = async (id: number, newBody: string): Promise<void> => {
    const result = await apiClient.patch<PostResponse>(`/posts/${String(id)}`, { body: newBody })
    setEditingPostId(null)
    setConversation((current) => {
      if (!current) return current
      if (current.target.post?.id === id) {
        return { ...current, target: { ...current.target, post: result.post } }
      }
      return {
        ...current,
        replies: current.replies.map((r) => (r.id === id ? result.post : r)),
      }
    })
  }

  const handleDelete = async (id: number): Promise<void> => {
    setIsDeleting(true)
    try {
      await apiClient.delete(`/posts/${String(id)}`)
      // If deleting the target post, navigate away
      if (conversation?.target.post?.id === id) {
        void navigate('/')
      } else {
        // Reload to get the soft-deleted placeholder
        await loadConversation()
      }
    } finally {
      setIsDeleting(false)
      setDeletingPostId(null)
    }
  }

  const handleLikeToggle = async (currentPost: PostSummary): Promise<void> => {
    if (isLikePending) return
    setIsLikePending(true)

    const optimisticLikedByViewer = !currentPost.likedByViewer
    const optimisticLikeCount = Math.max(0, currentPost.likeCount + (optimisticLikedByViewer ? 1 : -1))
    const optimistic = { ...currentPost, likedByViewer: optimisticLikedByViewer, likeCount: optimisticLikeCount }

    setConversation((current) => {
      if (!current) return current
      if (current.target.post?.id === currentPost.id) {
        return { ...current, target: { ...current.target, post: optimistic } }
      }
      return { ...current, replies: current.replies.map((r) => (r.id === currentPost.id ? optimistic : r)) }
    })

    try {
      const interactionState = currentPost.likedByViewer
        ? await apiClient.delete<PostInteractionState>(`/posts/${String(currentPost.id)}/like`)
        : await apiClient.post<PostInteractionState>(`/posts/${String(currentPost.id)}/like`)

      setConversation((current) => {
        if (!current) return current
        const update = (p: PostSummary): PostSummary =>
          p.id === currentPost.id
            ? { ...p, likedByViewer: interactionState.likedByViewer, likeCount: interactionState.likeCount }
            : p
        return {
          ...current,
          target: current.target.post ? { ...current.target, post: update(current.target.post) } : current.target,
          replies: current.replies.map(update),
        }
      })
    } catch {
      setConversation((current) => {
        if (!current) return current
        const revert = (p: PostSummary): PostSummary => (p.id === currentPost.id ? currentPost : p)
        return {
          ...current,
          target: current.target.post ? { ...current.target, post: revert(current.target.post) } : current.target,
          replies: current.replies.map(revert),
        }
      })
    } finally {
      setIsLikePending(false)
    }
  }

  const handleReplySubmit = async (): Promise<void> => {
    if (!postId || !replyBody.trim()) return
    setIsReplyPending(true)
    setReplyError(null)
    try {
      const result = await apiClient.post<PostResponse>(`/posts/${String(postId)}/replies`, { body: replyBody.trim() })
      setReplyBody('')
      setConversation((current) =>
        current ? { ...current, replies: [result.post, ...current.replies] } : current
      )
    } catch (err: unknown) {
      if (isApiError(err)) {
        setReplyError(err.detail !== undefined ? err.detail : err.title)
      } else {
        setReplyError('Failed to post reply. Please try again.')
      }
    } finally {
      setIsReplyPending(false)
    }
  }

  const targetAvailable = conversation?.target.state === 'available'

  if (isLoading) {
    return (
      <div className="page-loading" data-testid="post-page">
        <div className="text-center py-8" data-testid="conversation-status">
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
          <p className="empty-text">This post may have been deleted or does not exist.</p>
          <div style={{ marginTop: '24px' }}>
            <Button variant="primary" onClick={() => { void navigate('/') }} data-testid="post-unavailable-home-link">
              Back to Home
            </Button>
          </div>
        </div>
      </div>
    )
  }

  if (error && !conversation) {
    return (
      <div className="page-error-container" data-testid="post-page">
        <p className="page-error-text" data-testid="conversation-status">{error}</p>
        <div className="error-actions">
          <Button variant="primary" onClick={() => { void loadConversation() }}>Retry</Button>
          <Button variant="secondary" onClick={() => { void navigate('/') }}>Back to Home</Button>
        </div>
      </div>
    )
  }

  if (!conversation) return null

  return (
    <div className="post-detail-page" data-testid="post-page" aria-label="Conversation">
      <header className="page-header">
        <Button variant="ghost" onClick={() => { void navigate(-1) }} className="back-btn" data-testid="post-back-link">
          ←
        </Button>
        <h1 className="page-title">Post</h1>
      </header>

      <div data-testid="conversation-page">
      {targetAvailable && conversation.target.post ? (
        <div data-testid="conversation-target">
          {editingPostId === conversation.target.post.id ? (
            <PostEditor
              post={conversation.target.post}
              onSave={(body) => handleEdit(conversation.target.post?.id ?? 0, body)}
              onCancel={() => { setEditingPostId(null) }}
            />
          ) : (
            <PostCard
              post={conversation.target.post}
              isLikePending={isLikePending}
              showLikeButton={isAuthenticated}
              onLikeToggle={(p) => { void handleLikeToggle(p) }}
              onEdit={(p) => { setEditingPostId(p.id) }}
              onDelete={(p) => { setDeletingPostId(p.id) }}
            />
          )}
        </div>
      ) : (
        <div data-testid="conversation-target-unavailable" className="post-card post-card-deleted">
          <p className="post-deleted-text">This post is no longer available.</p>
        </div>
      )}

      {/* Reply composer */}
      {isAuthenticated && targetAvailable ? (
        <div className="reply-composer" data-testid="reply-composer">
          <textarea
            ref={replyInputRef}
            className="composer-textarea"
            data-testid="reply-composer-input"
            placeholder={`Reply to ${conversation.target.post?.authorUsername ?? 'post'}…`}
            value={replyBody}
            onChange={(e) => { setReplyBody(e.target.value) }}
            disabled={isReplyPending}
            rows={2}
          />
          {replyError ? (
            <p className="composer-error" role="alert" data-testid="reply-form-status">{replyError}</p>
          ) : null}
          <div className="composer-footer">
            <span className={`char-counter ${replyBody.length > 280 ? 'over-limit' : ''}`}>
              {280 - replyBody.length}
            </span>
            <Button
              variant="primary"
              onClick={() => { void handleReplySubmit() }}
              disabled={isReplyPending || !replyBody.trim() || replyBody.length > 280}
              data-testid="reply-submit-button"
            >
              {isReplyPending ? 'Posting…' : 'Reply'}
            </Button>
          </div>
        </div>
      ) : isAuthenticated && !targetAvailable ? (
        <div data-testid="reply-composer-unavailable" className="reply-composer-unavailable">
          <p>Replies are unavailable because the parent post no longer exists.</p>
        </div>
      ) : null}

      {/* Replies list */}
      <div className="conversation-replies" data-testid="conversation-replies">
        {conversation.replies.map((reply) => (
          reply.state === 'deleted' ? (
            <PostCard key={reply.id} post={reply} showLikeButton={false} showLikeCount={false} />
          ) : editingPostId === reply.id ? (
            <PostEditor
              key={reply.id}
              post={reply}
              onSave={(body) => handleEdit(reply.id, body)}
              onCancel={() => { setEditingPostId(null) }}
            />
          ) : (
            <PostCard
              key={reply.id}
              post={reply}
              isLikePending={isLikePending}
              showLikeButton={isAuthenticated}
              onLikeToggle={(p) => { void handleLikeToggle(p) }}
              onEdit={reply.canEdit ? (p) => { setEditingPostId(p.id) } : undefined}
              onDelete={reply.canDelete ? (p) => { setDeletingPostId(p.id) } : undefined}
            />
          )
        ))}
      </div>

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
        onCancel={() => { setDeletingPostId(null) }}
        isPending={isDeleting}
      />
    </div>
  )
}
