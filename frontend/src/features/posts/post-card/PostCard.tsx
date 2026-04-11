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
}: PostCardProps) {
  const navigate = useNavigate()

  const handleCardClick = () => {
    void navigate(`/posts/${String(post.id)}`)
  }

  const formatDate = (dateStr: string) => {
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

  return (
    <article
      data-testid={`post-card-${String(post.id)}`}
      className="post-card"
      onClick={handleCardClick}
    >
      <div className="post-card-avatar-column">
        <Link
          to={`/u/${post.authorUsername}`}
          onClick={(e) => {
            e.stopPropagation()
          }}
          data-testid={`author-link-${post.authorUsername}`}
        >
          <Avatar
            username={post.authorUsername}
            displayName={post.authorDisplayName}
            size="sm"
          />
        </Link>
      </div>

      <div className="post-card-content-column">
        <div className="post-header">
          <Link
            to={`/u/${post.authorUsername}`}
            onClick={(e) => {
              e.stopPropagation()
            }}
            className="author-display-name"
          >
            {post.authorDisplayName}
          </Link>
          <Link
            to={`/u/${post.authorUsername}`}
            onClick={(e) => {
              e.stopPropagation()
            }}
            className="author-username"
          >
            @{post.authorUsername}
          </Link>
          <span className="metadata-separator">·</span>
          <Link
            to={`/posts/${String(post.id)}`}
            onClick={(e) => {
              e.stopPropagation()
            }}
            data-testid={`post-permalink-${String(post.id)}`}
            className="post-timestamp"
          >
            <time data-testid={`post-timestamp-${String(post.id)}`}>
              {formatDate(post.createdAtUtc)}
            </time>
          </Link>
          {post.isEdited && (
            <span
              data-testid={`post-edited-badge-${String(post.id)}`}
              className="post-edited-indicator"
            >
              (edited)
            </span>
          )}
        </div>

        <div className="post-body" data-testid={`post-body-${String(post.id)}`}>
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
                  data-testid={`post-like-count-${String(post.id)}`}
                  className="action-count"
                >
                  {post.likeCount > 0 ? post.likeCount : ''}
                </span>
              </div>
            </div>
          ) : null}

          {post.canEdit && (
            <div className="post-action-item">
              <Button
                variant="ghost"
                onClick={(e) => {
                  e.stopPropagation()
                  onEdit?.(post)
                }}
                aria-label="Edit"
                data-testid={`post-edit-button-${String(post.id)}`}
                className="post-action-btn edit-btn"
              >
                ✏️
              </Button>
            </div>
          )}

          {post.canDelete && (
            <div className="post-action-item">
              <Button
                variant="ghost"
                onClick={(e) => {
                  e.stopPropagation()
                  onDelete?.(post)
                }}
                aria-label="Delete"
                data-testid={`post-delete-button-${String(post.id)}`}
                className="post-action-btn delete-btn"
              >
                🗑️
              </Button>
            </div>
          )}
        </div>
      </div>
    </article>
  )
}
