import { useState, FormEvent } from 'react'
import { apiClient } from '../../../shared/api/client'
import { isApiError } from '../../../shared/api/errors'

interface ComposerProps {
  onPostCreated?: () => void
}

export function Composer({ onPostCreated }: ComposerProps) {
  const [body, setBody] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isPending, setIsPending] = useState(false)

  const charCount = body.length
  const isOverLimit = charCount > 280
  const isEmpty = body.trim().length === 0

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (isEmpty || isOverLimit) return

    setIsPending(true)
    setError(null)

    try {
      await apiClient.post('/posts', { body: body.trim() })
      setBody('') // Clear on success
      onPostCreated?.()
    } catch (err) {
      if (isApiError(err)) {
        setError(err.detail || 'Failed to create post')
      } else {
        setError('An unexpected error occurred')
      }
      // Draft preserved (body not cleared)
    } finally {
      setIsPending(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} data-testid="composer-form">
      {error && <div role="alert">{error}</div>}

      <textarea
        data-testid="composer-textarea"
        value={body}
        onChange={(e) => setBody(e.target.value)}
        disabled={isPending}
        placeholder="What's happening?"
        rows={3}
      />

      <div>
        <span style={{ color: isOverLimit ? 'red' : 'gray' }}>
          {charCount}/280
        </span>

        <button
          type="submit"
          disabled={isPending || isEmpty || isOverLimit}
          data-testid="composer-submit"
        >
          {isPending ? 'Posting...' : 'Post'}
        </button>
      </div>

      {isOverLimit && (
        <div>Post exceeds 280 character limit</div>
      )}
    </form>
  )
}
