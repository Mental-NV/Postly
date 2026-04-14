import type { FormEvent} from 'react';
import { useState, useRef, useEffect } from 'react'
import { apiClient } from '../../../shared/api/client'
import { isApiError } from '../../../shared/api/errors'
import { Button } from '../../../shared/components/Button'
import { Avatar } from '../../../shared/components/Avatar'
import { useAuth } from '../../../app/providers/AuthContext'

interface ComposerProps {
  onPostCreated?: () => void
}

export function Composer({ onPostCreated }: ComposerProps): React.JSX.Element | null {
  const { session } = useAuth()
  const [body, setBody] = useState('')
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

  async function handleSubmit(e: FormEvent): Promise<void> {
    e.preventDefault()
    if (isEmpty || isOverLimit) return

    setIsPending(true)
    setError(null)

    try {
      await apiClient.post('/posts', { body: body.trim() })
      setBody('')
      onPostCreated?.()
    } catch (err) {
      if (isApiError(err)) {
        setError(err.detail || 'Failed to create post')
      } else {
        setError('An unexpected error occurred')
      }
    } finally {
      setIsPending(false)
    }
  }

  if (!session) return null

  return (
    <div className="composer-container">
      <div className="composer-avatar-column">
        <Avatar
          username={session.username}
          displayName={session.displayName}
          size="md"
        />
      </div>
      <form
        onSubmit={(e) => {
          void handleSubmit(e)
        }}
        className="composer-form"
        data-testid="composer-form"
      >
        <textarea
          ref={textareaRef}
          className="composer-textarea"
          data-testid="composer-textarea"
          value={body}
          onChange={(e) => { setBody(e.target.value); }}
          disabled={isPending}
          placeholder="What's happening?"
          rows={1}
        />

        {error ? <div className="composer-error" role="alert">
            {error}
          </div> : null}

        <div className="composer-footer">
          <div className="composer-stats">
            <span
              data-testid="composer-char-counter"
              className={`char-counter ${isOverLimit ? 'over-limit' : ''}`}
              style={{ opacity: isEmpty ? 0 : 1 }}
            >
              {280 - charCount}
            </span>
          </div>

          <Button
            type="submit"
            disabled={isPending || isEmpty || isOverLimit}
            data-testid="composer-submit"
          >
            {isPending ? 'Posting...' : 'Post'}
          </Button>
        </div>
      </form>
    </div>
  )
}
