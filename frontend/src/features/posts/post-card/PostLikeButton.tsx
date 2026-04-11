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
    <div className="flex items-center gap-3 text-sm text-gray-600">
      <button
        type="button"
        onClick={onToggle}
        disabled={isPending}
        aria-pressed={likedByViewer}
        data-testid={`post-like-button-${postId}`}
        className="rounded border border-gray-300 px-3 py-1 font-medium text-gray-700 disabled:cursor-not-allowed disabled:opacity-60"
      >
        {isPending ? (likedByViewer ? 'Unliking...' : 'Liking...') : likedByViewer ? 'Unlike' : 'Like'}
      </button>
      <span
        aria-live="polite"
        data-testid={`post-like-count-${postId}`}
        className="font-medium"
      >
        {likeCount} like{likeCount === 1 ? '' : 's'}
      </span>
    </div>
  )
}
