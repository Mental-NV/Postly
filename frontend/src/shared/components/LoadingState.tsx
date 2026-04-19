import { Button } from './Button'

interface LoadingStateProps {
  message?: string
}

export function LoadingState({ message = 'Loading...' }: LoadingStateProps): React.JSX.Element {
  return (
    <div
      data-testid="loading-state"
      role="status"
      aria-live="polite"
      style={{ padding: '2rem', textAlign: 'center' }}
    >
      <p>{message}</p>
    </div>
  )
}

interface ContinuationLoadingStateProps {
  message?: string
}

export function ContinuationLoadingState({
  message = 'Loading more content…',
}: ContinuationLoadingStateProps): React.JSX.Element {
  return (
    <div
      data-testid="collection-continuation-loading"
      role="status"
      aria-live="polite"
      style={{ padding: '1rem', textAlign: 'center' }}
    >
      <p>{message}</p>
    </div>
  )
}

interface ContinuationErrorStateProps {
  message: string
  onRetry: () => void
}

export function ContinuationErrorState({
  message,
  onRetry,
}: ContinuationErrorStateProps): React.JSX.Element {
  return (
    <div
      data-testid="collection-continuation-error"
      role="alert"
      style={{ padding: '1rem', textAlign: 'center' }}
    >
      <p>{message}</p>
      <Button
        type="button"
        variant="secondary"
        onClick={onRetry}
        data-testid="collection-continuation-retry"
      >
        Retry
      </Button>
    </div>
  )
}

interface ContinuationEndStateProps {
  message?: string
}

export function ContinuationEndState({
  message = 'No more posts to show.',
}: ContinuationEndStateProps): React.JSX.Element {
  return (
    <div
      data-testid="collection-end-state"
      role="status"
      aria-live="polite"
      style={{ padding: '1rem', textAlign: 'center' }}
    >
      <p>{message}</p>
    </div>
  )
}
