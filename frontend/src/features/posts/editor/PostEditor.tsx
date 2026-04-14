import { useState, useRef, useEffect } from 'react'
import type { PostSummary } from '../../../shared/api/contracts'
import { isApiError } from '../../../shared/api/errors'
import { Button } from '../../../shared/components/Button'

interface PostEditorProps {
  post: PostSummary
  onSave: (body: string) => Promise<void>
  onCancel: () => void
}

export function PostEditor({ post, onSave, onCancel }: PostEditorProps) {
  const [body, setBody] = useState(post.body)
  const [error, setError] = useState<string | null>(null)
  const [isPending, setIsPending] = useState(false)
  const textareaRef = useRef<HTMLTextAreaElement>(null)

  const charCount = body.length
  const isOverLimit = charCount > 280
  const isEmpty = body.trim().length === 0

  // Auto-resize textarea
  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto'
      textareaRef.current.style.height = `${textareaRef.current.scrollHeight}px`
    }
  }, [body])

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
    <div
      className="post-editor-container"
      data-testid="post-editor"
      onClick={(e) => { e.stopPropagation(); }}
    >
      <textarea
        ref={textareaRef}
        className="composer-textarea editor-textarea"
        data-testid="editor-textarea"
        value={body}
        onChange={(e) => { setBody(e.target.value); }}
        disabled={isPending}
        rows={1}
        autoFocus
      />

      {error ? <div className="composer-error" role="alert">
          {error}
        </div> : null}

      <div className="composer-footer">
        <div className="composer-stats">
          <span className={`char-counter ${isOverLimit ? 'over-limit' : ''}`}>
            {280 - charCount}
          </span>
        </div>

        <div className="editor-actions">
          <Button
            variant="ghost"
            onClick={() => {
              onCancel()
            }}
            disabled={isPending}
          >
            Cancel
          </Button>
          <Button
            variant="primary"
            onClick={() => {
              void handleSave()
            }}
            disabled={isPending || isEmpty || isOverLimit}
            data-testid="editor-save"
          >
            {isPending ? 'Saving...' : 'Save'}
          </Button>
        </div>
      </div>
    </div>
  )
}
