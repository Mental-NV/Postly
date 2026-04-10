interface LoadingStateProps {
  message?: string
}

export function LoadingState({ message = 'Loading...' }: LoadingStateProps) {
  return (
    <div data-testid="loading-state" style={{ padding: '2rem', textAlign: 'center' }}>
      <p>{message}</p>
    </div>
  )
}
