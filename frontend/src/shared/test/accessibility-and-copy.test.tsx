import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { NotificationUnavailablePage } from '../../features/notifications/NotificationUnavailablePage'
import { PostCard } from '../../features/posts/post-card/PostCard'
import { Avatar } from '../components/Avatar'
import { LoadingState } from '../components/LoadingState'
import { ErrorState } from '../components/ErrorState'
import { EmptyState } from '../components/EmptyState'
import { createMockPost } from './factories'

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

describe('shared avatar rendering', () => {
  it('renders avatar fallback initials when no image is available', () => {
    render(
      <Avatar
        username="alice"
        displayName="Alice Example"
        avatarUrl={null}
        fallbackTestId="avatar-fallback"
      />
    )

    expect(screen.getByTestId('avatar-fallback')).toHaveTextContent('AE')
    expect(screen.queryByRole('img', { name: 'Alice Example' })).not.toBeInTheDocument()
  })

  it('renders avatar image when an avatar URL is provided', () => {
    render(
      <Avatar
        username="bob"
        displayName="Bob Tester"
        avatarUrl="/api/profiles/bob/avatar?v=2"
        imageTestId="avatar-image"
      />
    )

    expect(screen.getByTestId('avatar-image')).toHaveAttribute(
      'src',
      '/api/profiles/bob/avatar?v=2'
    )
    expect(screen.getByRole('img', { name: 'Bob Tester' })).toBeInTheDocument()
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
    expect(screen.getByTestId('post-avatar-42')).toBeInTheDocument()
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
    expect(screen.getByTestId('post-edit-button-99')).toBeInTheDocument()
    expect(screen.getByTestId('post-delete-button-99')).toBeInTheDocument()
    expect(screen.getByTestId('post-like-count-99')).toHaveTextContent('1')

    await user.click(screen.getByRole('button', { name: 'Unlike' }))
    await user.click(screen.getByRole('button', { name: 'Edit' }))
    await user.click(screen.getByRole('button', { name: 'Delete' }))

    expect(onLikeToggle).toHaveBeenCalledOnce()
    expect(onEdit).toHaveBeenCalledOnce()
    expect(onDelete).toHaveBeenCalledOnce()
  })

  it('renders a read-only like count without a like button when requested', () => {
    render(
      <MemoryRouter>
        <PostCard
          post={createMockPost({
            id: 77,
            authorUsername: 'alice',
            authorDisplayName: 'Alice Example',
            likeCount: 5,
            likedByViewer: false,
          })}
          showLikeButton={false}
        />
      </MemoryRouter>
    )

    expect(screen.queryByRole('button', { name: 'Like' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Unlike' })).not.toBeInTheDocument()
    expect(screen.getByTestId('post-like-count-77')).toHaveTextContent('5')
  })

  it('renders deleted reply placeholder copy without interactive controls', () => {
    render(
      <MemoryRouter>
        <PostCard
          post={createMockPost({
            id: 88,
            state: 'deleted',
            body: null,
            authorUsername: null,
            authorDisplayName: null,
            canEdit: false,
            canDelete: false,
          })}
        />
      </MemoryRouter>
    )

    expect(
      screen.getByTestId('deleted-reply-placeholder-88')
    ).toHaveTextContent('This reply was deleted by the author.')
    expect(
      screen.queryByTestId('post-edit-button-88')
    ).not.toBeInTheDocument()
    expect(
      screen.queryByTestId('post-delete-button-88')
    ).not.toBeInTheDocument()
  })
})

describe('shared unavailable-state copy', () => {
  it('renders notification unavailable page copy and return navigation', () => {
    render(
      <MemoryRouter>
        <NotificationUnavailablePage />
      </MemoryRouter>
    )

    expect(
      screen.getByTestId('notification-unavailable-page')
    ).toBeInTheDocument()
    expect(screen.getByText('Content Not Available')).toBeInTheDocument()
    expect(
      screen.getByText("The content you're looking for is no longer available.")
    ).toBeInTheDocument()
    expect(
      screen.getByRole('link', { name: /Back to Notifications/ })
    ).toHaveAttribute('href', '/notifications')
  })
})
