import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { LoadingState } from '../components/LoadingState'
import { ErrorState } from '../components/ErrorState'
import { EmptyState } from '../components/EmptyState'
import { createMockPost } from './factories'
import { PostCard } from '../../features/posts/post-card/PostCard'

describe('shared route states accessibility and copy', () => {
  it('renders loading state as a polite status message', () => {
    render(<LoadingState message="Loading timeline..." />)

    const status = screen.getByRole('status')
    expect(status).toHaveTextContent('Loading timeline...')
    expect(status).toHaveAttribute('aria-live', 'polite')
  })

  it('renders error state as an alert with a clear retry action', async () => {
    const onRetry = vi.fn()
    const user = userEvent.setup()

    render(
      <ErrorState
        message="Failed to load timeline. Please try again."
        onRetry={onRetry}
      />
    )

    expect(screen.getByRole('alert')).toHaveTextContent(
      'Failed to load timeline. Please try again.'
    )

    const retryButton = screen.getByRole('button', { name: 'Retry' })
    await user.click(retryButton)

    expect(onRetry).toHaveBeenCalledOnce()
  })

  it('renders empty state with explicit next-action copy', async () => {
    const onCreateFirstPost = vi.fn()
    const user = userEvent.setup()

    render(
      <EmptyState
        message="No posts are available yet."
        action={{ label: 'Create your first post', onClick: onCreateFirstPost }}
      />
    )

    expect(screen.getByRole('status')).toHaveTextContent(
      'No posts are available yet.'
    )

    const actionButton = screen.getByRole('button', {
      name: 'Create your first post',
    })
    await user.click(actionButton)

    expect(onCreateFirstPost).toHaveBeenCalledOnce()
  })
})

describe('shared post card accessibility and copy', () => {
  it('renders readable post identity, body, permalink, and like copy', () => {
    render(
      <MemoryRouter>
        <PostCard
          post={createMockPost({
            id: 42,
            authorUsername: 'alice',
            authorDisplayName: 'Alice Example',
            body: 'Seed post from Alice',
            likeCount: 2,
            likedByViewer: false,
          })}
        />
      </MemoryRouter>
    )

    expect(
      screen.getByTestId('author-link-alice')
    ).toBeInTheDocument()
    expect(screen.getByText('@alice')).toBeInTheDocument()
    expect(screen.getByTestId('post-body-42')).toHaveTextContent(
      'Seed post from Alice'
    )
    expect(screen.getByTestId('post-permalink-42')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Like' })).toHaveAttribute(
      'aria-pressed',
      'false'
    )
    expect(screen.getByTestId('post-like-count-42')).toHaveTextContent(
      '2'
    )
    expect(
      screen.queryByRole('button', { name: 'Edit' })
    ).not.toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: 'Delete' })
    ).not.toBeInTheDocument()
  })

  it('renders explicit edited and owner-only action labels', async () => {
    const onLikeToggle = vi.fn()
    const onEdit = vi.fn()
    const onDelete = vi.fn()
    const user = userEvent.setup()

    render(
      <MemoryRouter>
        <PostCard
          post={createMockPost({
            id: 99,
            authorUsername: 'bob',
            authorDisplayName: 'Bob Tester',
            isEdited: true,
            likeCount: 1,
            likedByViewer: true,
            canEdit: true,
            canDelete: true,
          })}
          onLikeToggle={onLikeToggle}
          onEdit={onEdit}
          onDelete={onDelete}
        />
      </MemoryRouter>
    )

    expect(screen.getByTestId('post-edited-badge-99')).toHaveTextContent(
      '(edited)'
    )
    expect(screen.getByRole('button', { name: 'Unlike' })).toHaveAttribute(
      'aria-pressed',
      'true'
    )
    expect(screen.getByTestId('post-like-count-99')).toHaveTextContent('1')

    await user.click(screen.getByRole('button', { name: 'Unlike' }))
    await user.click(screen.getByRole('button', { name: 'Edit' }))
    await user.click(screen.getByRole('button', { name: 'Delete' }))

    expect(onLikeToggle).toHaveBeenCalledOnce()
    expect(onEdit).toHaveBeenCalledOnce()
    expect(onDelete).toHaveBeenCalledOnce()
  })
})
