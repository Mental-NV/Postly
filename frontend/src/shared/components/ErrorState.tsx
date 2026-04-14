interface ErrorStateProps {
  message: string
  onRetry?: () => void
}

export function ErrorState({ message, onRetry }: ErrorStateProps) {
  return (
    <div
      data-testid="error-state"
      role="alert"
      style={{ padding: '2rem', textAlign: 'center' }}
    >
      <p style={{ color: 'red' }}>{message}</p>
      {onRetry ? <button type="button" onClick={onRetry} data-testid="retry-button">
          Retry
        </button> : null}
    </div>
  )
}
