import { Link } from 'react-router-dom'
import type { PostSummary } from '../../../shared/api/contracts'
import { PostLikeButton } from './PostLikeButton'

interface PostCardProps {
  post: PostSummary
  isLikePending?: boolean
  onLikeToggle?: (post: PostSummary) => void
  onEdit?: (post: PostSummary) => void
  onDelete?: (post: PostSummary) => void
}

function getInitials(displayName: string) {
  return displayName
    .split(' ')
    .map((word) => word[0])
    .join('')
    .toUpperCase()
    .slice(0, 2)
}

export function PostCard({
  post,
  isLikePending = false,
  onLikeToggle,
  onEdit,
  onDelete,
}: PostCardProps) {
  return (
    <article
      data-testid={`post-card-${post.id}`}
      className="rounded-lg bg-white p-4 shadow"
    >
      <div className="flex items-start gap-3">
        <Link
          to={`/u/${post.authorUsername}`}
          data-testid={`author-link-${post.authorUsername}`}
          className="flex items-start gap-3 hover:opacity-90"
        >
          <div className="flex h-10 w-10 items-center justify-center rounded-full bg-gradient-to-br from-blue-400 to-purple-500 font-bold text-white">
            {getInitials(post.authorDisplayName)}
          </div>
        </Link>

        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <Link
              to={`/u/${post.authorUsername}`}
              className="font-bold hover:underline"
            >
              {post.authorDisplayName}
            </Link>
            <Link
              to={`/u/${post.authorUsername}`}
              className="text-gray-600 hover:underline"
            >
              @{post.authorUsername}
            </Link>
            <span className="text-gray-400">·</span>
            <Link
              to={`/posts/${post.id}`}
              data-testid={`post-permalink-${post.id}`}
              className="text-sm text-gray-600 hover:underline"
            >
              <time data-testid={`post-timestamp-${post.id}`}>
                {new Date(post.createdAtUtc).toLocaleString()}
              </time>
            </Link>
            {post.isEdited && (
              <span
                data-testid={`post-edited-badge-${post.id}`}
                className="text-sm text-gray-500"
              >
                (edited)
              </span>
            )}
          </div>

          <p
            data-testid={`post-body-${post.id}`}
            className="mt-2 whitespace-pre-wrap text-gray-900"
          >
            {post.body}
          </p>

          <div className="mt-3 flex flex-wrap items-center gap-3">
            <PostLikeButton
              postId={post.id}
              likedByViewer={post.likedByViewer}
              likeCount={post.likeCount}
              isPending={isLikePending}
              onToggle={() => onLikeToggle?.(post)}
            />

            {post.canEdit && (
              <button
                type="button"
                onClick={() => onEdit?.(post)}
                data-testid={`post-edit-button-${post.id}`}
                className="rounded bg-blue-100 px-3 py-1 text-sm font-medium text-blue-700"
              >
                Edit
              </button>
            )}

            {post.canDelete && (
              <button
                type="button"
                onClick={() => onDelete?.(post)}
                data-testid={`post-delete-button-${post.id}`}
                className="rounded bg-red-100 px-3 py-1 text-sm font-medium text-red-700"
              >
                Delete
              </button>
            )}
          </div>
        </div>
      </div>
    </article>
  )
}
