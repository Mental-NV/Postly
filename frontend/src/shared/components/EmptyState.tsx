interface EmptyStateProps {
  message: string
  action?: {
    label: string
    onClick: () => void
  }
}

export function EmptyState({ message, action }: EmptyStateProps) {
  return (
    <div data-testid="empty-state" style={{ padding: '2rem', textAlign: 'center' }}>
      <p>{message}</p>
      {action && (
        <button onClick={action.onClick} data-testid="empty-state-action">
          {action.label}
        </button>
      )}
    </div>
  )
}
