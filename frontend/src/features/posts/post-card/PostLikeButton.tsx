import { Button } from '../../../shared/components/Button'

interface PostLikeButtonProps {
  postId: number
  likedByViewer: boolean
  likeCount: number
  isPending: boolean
  onToggle: () => void
}

export function PostLikeButton({
  postId,
  likedByViewer,
  likeCount,
  isPending,
  onToggle,
}: PostLikeButtonProps) {
  return (
    <div className="post-action-item like-action">
      <Button
        variant="ghost"
        onClick={(e) => {
          e.stopPropagation()
          onToggle()
        }}
        disabled={isPending}
        aria-pressed={likedByViewer}
        data-testid={`post-like-button-${postId}`}
        className={`post-action-btn like-btn ${likedByViewer ? 'liked' : ''}`}
      >
        <span className="action-icon">{likedByViewer ? '❤️' : '🤍'}</span>
        <span
          aria-live="polite"
          data-testid={`post-like-count-${postId}`}
          className="action-count"
        >
          {likeCount > 0 ? likeCount : ''}
        </span>
      </Button>
    </div>
  )
}
