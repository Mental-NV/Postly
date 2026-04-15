import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Composer } from '../composer/Composer'
import { PostEditor } from '../editor/PostEditor'
import { ConfirmDialog } from '../../../shared/components/ConfirmDialog'
import { apiClient } from '../../../shared/api/client'
import { ApiError } from '../../../shared/api/errors'
import {
  renderWithProviders,
  mockAuthenticatedSession,
} from '../../../shared/test/helpers'

vi.mock('../../../shared/api/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
  },
}))

describe('Composer', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders an empty composer with submission disabled', () => {
    renderWithProviders(<Composer />, { session: mockAuthenticatedSession() })

    expect(screen.getByTestId('composer-textarea')).toBeInTheDocument()
    expect(screen.getByTestId('composer-submit')).toBeInTheDocument()
    expect(screen.getByTestId('composer-submit')).toBeDisabled()
  })

  it('updates the remaining character count as the draft changes', async () => {
    const user = userEvent.setup()
    renderWithProviders(<Composer />, { session: mockAuthenticatedSession() })

    const textarea = screen.getByTestId('composer-textarea')
    await user.type(textarea, 'Hello')

    expect(screen.getByText('275')).toBeInTheDocument()
  })

  it('prevents submission when the draft exceeds the character limit', async () => {
    const user = userEvent.setup()
    renderWithProviders(<Composer />, { session: mockAuthenticatedSession() })

    const textarea = screen.getByTestId('composer-textarea')
    await user.type(textarea, 'a'.repeat(281))

    expect(screen.getByTestId('composer-submit')).toBeDisabled()
    expect(screen.getByText('-1')).toBeInTheDocument()
  })

  it('clears textarea after successful submit', async () => {
    const user = userEvent.setup()
    vi.mocked(apiClient.post).mockResolvedValueOnce({})

    renderWithProviders(<Composer />, { session: mockAuthenticatedSession() })

    const textarea = screen.getByTestId('composer-textarea')
    await user.type(textarea, 'Test post')
    await user.click(screen.getByTestId('composer-submit'))

    await waitFor(() => {
      expect(textarea).toHaveValue('')
    })
  })

  it('preserves draft after failed submit', async () => {
    const user = userEvent.setup()
    vi.mocked(apiClient.post).mockRejectedValueOnce(
      new ApiError(500, 'error', 'Server error', 'Server error')
    )

    renderWithProviders(<Composer />, { session: mockAuthenticatedSession() })

    const textarea = screen.getByTestId('composer-textarea')
    await user.type(textarea, 'Test post')
    await user.click(screen.getByTestId('composer-submit'))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument()
    })

    expect(textarea).toHaveValue('Test post')
  })

  it('shows pending state during submission', async () => {
    const user = userEvent.setup()
    vi.mocked(apiClient.post).mockImplementationOnce(
      () => new Promise((resolve) => setTimeout(resolve, 100))
    )

    renderWithProviders(<Composer />, { session: mockAuthenticatedSession() })

    const textarea = screen.getByTestId('composer-textarea')
    await user.type(textarea, 'Test post')

    const submitButton = screen.getByTestId('composer-submit')
    await user.click(submitButton)

    expect(submitButton).toBeDisabled()
    expect(submitButton).toHaveTextContent('Posting...')
  })

  it('calls onPostCreated after successful submit', async () => {
    const user = userEvent.setup()
    const onPostCreated = vi.fn()
    vi.mocked(apiClient.post).mockResolvedValueOnce({})

    renderWithProviders(<Composer onPostCreated={onPostCreated} />, {
      session: mockAuthenticatedSession(),
    })

    const textarea = screen.getByTestId('composer-textarea')
    await user.type(textarea, 'Test post')
    await user.click(screen.getByTestId('composer-submit'))

    await waitFor(() => {
      expect(onPostCreated).toHaveBeenCalled()
    })
  })
})

describe('PostEditor', () => {
  const mockPost = {
    id: 1,
    authorUsername: 'alice',
    authorDisplayName: 'Alice',
    body: 'Original content',
    createdAtUtc: new Date().toISOString(),
    isEdited: false,
    likeCount: 0,
    likedByViewer: false,
    canEdit: true,
    canDelete: true,
    isReply: false,
    replyToPostId: null,
    state: 'available' as const,
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders the existing post content and remaining character count', () => {
    render(<PostEditor post={mockPost} onSave={vi.fn()} onCancel={vi.fn()} />)

    expect(screen.getByTestId('post-editor-body-input-1')).toHaveValue(
      'Original content'
    )
    expect(screen.getByText('264')).toBeInTheDocument()
  })

  it('validates character limit', async () => {
    const user = userEvent.setup()
    render(<PostEditor post={mockPost} onSave={vi.fn()} onCancel={vi.fn()} />)

    const textarea = screen.getByTestId('post-editor-body-input-1')
    await user.clear(textarea)
    await user.type(textarea, 'a'.repeat(281))

    expect(screen.getByTestId('post-editor-save-button-1')).toBeDisabled()
  })

  it('calls onSave with updated content', async () => {
    const user = userEvent.setup()
    const onSave = vi.fn().mockResolvedValue(undefined)

    render(<PostEditor post={mockPost} onSave={onSave} onCancel={vi.fn()} />)

    const textarea = screen.getByTestId('post-editor-body-input-1')
    await user.clear(textarea)
    await user.type(textarea, 'Updated content')
    await user.click(screen.getByTestId('post-editor-save-button-1'))

    await waitFor(() => {
      expect(onSave).toHaveBeenCalledWith('Updated content')
    })
  })

  it('calls onCancel without saving', async () => {
    const user = userEvent.setup()
    const onCancel = vi.fn()

    render(<PostEditor post={mockPost} onSave={vi.fn()} onCancel={onCancel} />)

    await user.click(screen.getByText('Cancel'))

    expect(onCancel).toHaveBeenCalled()
  })

  it('shows pending state during save', async () => {
    const user = userEvent.setup()
    const onSave = vi
      .fn()
      .mockImplementationOnce(
        () => new Promise((resolve) => setTimeout(resolve, 100))
      )

    render(<PostEditor post={mockPost} onSave={onSave} onCancel={vi.fn()} />)

    const saveButton = screen.getByTestId('post-editor-save-button-1')
    await user.click(saveButton)

    expect(saveButton).toBeDisabled()
    expect(saveButton).toHaveTextContent('Saving...')
  })

  it('shows error message on save failure', async () => {
    const user = userEvent.setup()
    const onSave = vi
      .fn()
      .mockRejectedValueOnce(
        new ApiError(500, 'error', 'Server error', 'Failed to update post')
      )

    render(<PostEditor post={mockPost} onSave={onSave} onCancel={vi.fn()} />)

    await user.click(screen.getByTestId('post-editor-save-button-1'))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(
        'Failed to update post'
      )
    })
  })
})

describe('ConfirmDialog', () => {
  it('shows dialog when open', () => {
    render(
      <ConfirmDialog
        isOpen
        title="Delete Post"
        message="Are you sure?"
        onConfirm={vi.fn()}
        onCancel={vi.fn()}
      />
    )

    expect(screen.getByRole('dialog')).toBeInTheDocument()
    expect(screen.getByText('Delete Post')).toBeInTheDocument()
    expect(screen.getByText('Are you sure?')).toBeInTheDocument()
  })

  it('calls onConfirm when confirmed', async () => {
    const user = userEvent.setup()
    const onConfirm = vi.fn()

    render(
      <ConfirmDialog
        isOpen
        title="Delete Post"
        message="Are you sure?"
        onConfirm={onConfirm}
        onCancel={vi.fn()}
      />
    )

    await user.click(screen.getByTestId('confirm-delete'))

    expect(onConfirm).toHaveBeenCalled()
  })

  it('calls onCancel when cancelled', async () => {
    const user = userEvent.setup()
    const onCancel = vi.fn()

    render(
      <ConfirmDialog
        isOpen
        title="Delete Post"
        message="Are you sure?"
        onConfirm={vi.fn()}
        onCancel={onCancel}
      />
    )

    await user.click(screen.getByText('Cancel'))

    expect(onCancel).toHaveBeenCalled()
  })

  it('does not render when closed', () => {
    render(
      <ConfirmDialog
        isOpen={false}
        title="Delete Post"
        message="Are you sure?"
        onConfirm={vi.fn()}
        onCancel={vi.fn()}
      />
    )

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('shows pending state during deletion', () => {
    render(
      <ConfirmDialog
        isOpen
        title="Delete Post"
        message="Are you sure?"
        onConfirm={vi.fn()}
        onCancel={vi.fn()}
        isPending
      />
    )

    const confirmButton = screen.getByTestId('confirm-delete')
    expect(confirmButton).toBeDisabled()
    expect(confirmButton).toHaveTextContent('Deleting...')
  })

  it('uses custom button text', () => {
    render(
      <ConfirmDialog
        isOpen
        title="Delete Post"
        message="Are you sure?"
        confirmText="Yes, delete"
        cancelText="No, keep it"
        onConfirm={vi.fn()}
        onCancel={vi.fn()}
      />
    )

    expect(screen.getByText('Yes, delete')).toBeInTheDocument()
    expect(screen.getByText('No, keep it')).toBeInTheDocument()
  })
})
