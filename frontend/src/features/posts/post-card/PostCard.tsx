import { Link, useNavigate } from 'react-router-dom'
import type { PostSummary } from '../../../shared/api/contracts'
import { PostLikeButton } from './PostLikeButton'
import { Avatar } from '../../../shared/components/Avatar'
import { Button } from '../../../shared/components/Button'

interface PostCardProps {
  post: PostSummary
  isLikePending?: boolean
  showLikeButton?: boolean
  showLikeCount?: boolean
  onLikeToggle?: (post: PostSummary) => void
  onEdit?: (post: PostSummary) => void
  onDelete?: (post: PostSummary) => void
}

export function PostCard({
  post,
  isLikePending = false,
  showLikeButton = true,
  showLikeCount = true,
  onLikeToggle,
  onEdit,
  onDelete,
}: PostCardProps): React.JSX.Element {
  const navigate = useNavigate()

  const handleCardClick = (): void => {
    if (post.state === 'deleted') return
    void navigate(`/posts/${post.id}`)
  }

  const handleKeyDown = (e: React.KeyboardEvent): void => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault()
      handleCardClick()
    }
  }

  const formatDate = (dateStr: string): string => {
    const date = new Date(dateStr)
    const now = new Date()
    const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000)

    if (diffInSeconds < 60) return 'Just now'
    if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m`
    if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h`

    return date.toLocaleDateString(undefined, {
      month: 'short',
      day: 'numeric',
    })
  }

  // Deleted reply placeholder — non-interactive, no author/body
  if (post.state === 'deleted') {
    return (
      <article
        data-testid={`deleted-reply-placeholder-${post.id}`}
        className="post-card post-card-deleted"
      >
        <div className="post-card-content-column">
          <p className="post-deleted-text">This reply was deleted by the author.</p>
        </div>
      </article>
    )
  }

  return (
    <article
      data-testid={`post-card-${post.id}`}
      className="post-card"
      onClick={handleCardClick}
      onKeyDown={handleKeyDown}
      role="button"
      tabIndex={0}
    >
      <div className="post-card-avatar-column">
        <Link
          to={`/u/${post.authorUsername ?? ''}`}
          onClick={(e) => {
            e.stopPropagation()
          }}
          data-testid={`author-link-${post.authorUsername ?? ''}`}
        >
          <Avatar
            username={post.authorUsername ?? ''}
            displayName={post.authorDisplayName ?? ''}
            avatarUrl={post.authorAvatarUrl}
            size="sm"
            wrapperTestId={`post-avatar-${post.id}`}
          />
        </Link>
      </div>

      <div className="post-card-content-column">
        <div className="post-header">
          <Link
            to={`/u/${post.authorUsername ?? ''}`}
            onClick={(e) => {
              e.stopPropagation()
            }}
            className="author-display-name"
          >
            {post.authorDisplayName}
          </Link>
          <Link
            to={`/u/${post.authorUsername ?? ''}`}
            onClick={(e) => {
              e.stopPropagation()
            }}
            className="author-username"
          >
            @{post.authorUsername}
          </Link>
          <span className="metadata-separator">·</span>
          <Link
            to={`/posts/${post.id}`}
            onClick={(e) => {
              e.stopPropagation()
            }}
            data-testid={`post-permalink-${post.id}`}
            className="post-timestamp"
          >
            <time data-testid={`post-timestamp-${post.id}`}>
              {formatDate(post.createdAtUtc)}
            </time>
          </Link>
          {post.isEdited ? <span
              data-testid={`post-edited-badge-${post.id}`}
              className="post-edited-indicator"
            >
              (edited)
            </span> : null}
        </div>

        <div className="post-body" data-testid={`post-body-${post.id}`}>
          {post.body}
        </div>

        <div className="post-actions">
          {showLikeButton ? (
            <PostLikeButton
              postId={post.id}
              likedByViewer={post.likedByViewer}
              likeCount={post.likeCount}
              isPending={isLikePending}
              onToggle={() => {
                onLikeToggle?.(post)
              }}
            />
          ) : showLikeCount ? (
            <div className="post-action-item like-action">
              <div className="post-action-btn" aria-hidden="true">
                <span className="action-icon">🤍</span>
                <span
                  aria-live="polite"
                  data-testid={`post-like-count-${post.id}`}
                  className="action-count"
                >
                  {post.likeCount > 0 ? post.likeCount : ''}
                </span>
              </div>
            </div>
          ) : null}

          {post.canEdit ? <div className="post-action-item">
              <Button
                variant="ghost"
                onClick={(e) => {
                  e.stopPropagation()
                  onEdit?.(post)
                }}
                aria-label="Edit"
                data-testid={`post-edit-button-${post.id}`}
                className="post-action-btn edit-btn"
              >
                ✏️
              </Button>
            </div> : null}

          {post.canDelete ? <div className="post-action-item">
              <Button
                variant="ghost"
                onClick={(e) => {
                  e.stopPropagation()
                  onDelete?.(post)
                }}
                aria-label="Delete"
                data-testid={`post-delete-button-${post.id}`}
                className="post-action-btn delete-btn"
              >
                🗑️
              </Button>
            </div> : null}
        </div>
      </div>
    </article>
  )
}
