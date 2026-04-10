import { useState } from 'react'
import { isApiError } from '../../../shared/api/errors'

interface PostSummary {
  id: number
  authorUsername: string
  authorDisplayName: string
  body: string
  createdAtUtc: string
  isEdited: boolean
  editedAtUtc?: string
  likeCount: number
  likedByViewer: boolean
  canEdit: boolean
  canDelete: boolean
}

interface PostEditorProps {
  post: PostSummary
  onSave: (body: string) => Promise<void>
  onCancel: () => void
}

export function PostEditor({ post, onSave, onCancel }: PostEditorProps) {
  const [body, setBody] = useState(post.body)
  const [error, setError] = useState<string | null>(null)
  const [isPending, setIsPending] = useState(false)

  const charCount = body.length
  const isOverLimit = charCount > 280
  const isEmpty = body.trim().length === 0

  async function handleSave() {
    if (isEmpty || isOverLimit) return

    setIsPending(true)
    setError(null)

    try {
      await onSave(body.trim())
    } catch (err) {
      if (isApiError(err)) {
        setError(err.detail || 'Failed to update post')
      } else {
        setError('An unexpected error occurred')
      }
    } finally {
      setIsPending(false)
    }
  }

  return (
    <div data-testid="post-editor">
      {error && <div role="alert">{error}</div>}

      <textarea
        data-testid="editor-textarea"
        value={body}
        onChange={(e) => setBody(e.target.value)}
        disabled={isPending}
        rows={3}
      />

      <div>
        <span style={{ color: isOverLimit ? 'red' : 'gray' }}>
          {charCount}/280
        </span>

        <button onClick={onCancel} disabled={isPending}>
          Cancel
        </button>
        <button
          onClick={handleSave}
          disabled={isPending || isEmpty || isOverLimit}
          data-testid="editor-save"
        >
          {isPending ? 'Saving...' : 'Save'}
        </button>
      </div>
    </div>
  )
}
