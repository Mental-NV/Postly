import type { PostSummary } from '../api/contracts'

interface PostCardProps {
  post: PostSummary
  onEdit?: (postId: number) => void
  onDelete?: (postId: number) => void
  onLike?: (postId: number) => void
  onUnlike?: (postId: number) => void
}

export function PostCard({
  post,
  onEdit,
  onDelete,
  onLike,
  onUnlike,
}: PostCardProps) {
  return (
    <div
      data-testid="post-card"
      style={{ border: '1px solid #ccc', padding: '1rem', margin: '0.5rem 0' }}
    >
      <div>
        <strong>{post.authorDisplayName}</strong> @{post.authorUsername}
      </div>
      <p>{post.body}</p>
      <div>
        <small>
          {new Date(post.createdAtUtc).toLocaleString()}
          {post.isEdited ? ' (edited)' : null}
        </small>
      </div>
      <div>
        <span>❤️ {post.likeCount}</span>
        {post.likedByViewer ? (
          <button onClick={() => onUnlike?.(post.id)}>Unlike</button>
        ) : (
          <button onClick={() => onLike?.(post.id)}>Like</button>
        )}
        {post.canEdit ? <button onClick={() => onEdit?.(post.id)}>Edit</button> : null}
        {post.canDelete ? <button onClick={() => onDelete?.(post.id)}>Delete</button> : null}
      </div>
    </div>
  )
}
