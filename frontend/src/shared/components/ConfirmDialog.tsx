interface ConfirmDialogProps {
  isOpen: boolean
  title: string
  message: string
  confirmText?: string
  cancelText?: string
  onConfirm: () => void
  onCancel: () => void
  isPending?: boolean
}

export function ConfirmDialog({
  isOpen,
  title,
  message,
  confirmText = 'Confirm',
  cancelText = 'Cancel',
  onConfirm,
  onCancel,
  isPending = false,
}: ConfirmDialogProps): React.JSX.Element | null {
  if (!isOpen) return null

  return (
    <div
      role="dialog"
      data-testid="confirm-dialog"
      style={{
        position: 'fixed',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        backgroundColor: 'rgba(0,0,0,0.5)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
      }}
    >
      <div
        style={{
          backgroundColor: 'white',
          padding: '2rem',
          borderRadius: '8px',
        }}
      >
        <h2>{title}</h2>
        <p>{message}</p>
        <div>
          <button onClick={onCancel} disabled={isPending}>
            {cancelText}
          </button>
          <button
            onClick={onConfirm}
            disabled={isPending}
            data-testid="confirm-delete"
          >
            {isPending ? 'Deleting...' : confirmText}
          </button>
        </div>
      </div>
    </div>
  )
}
