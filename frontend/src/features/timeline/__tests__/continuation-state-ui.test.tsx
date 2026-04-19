import { act, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { TimelinePage } from '../TimelinePage'
import { createMockTimeline, createMockPost } from '../../../shared/test/factories'
import { apiClient } from '../../../shared/api/client'
import {
  mockAuthenticatedSession,
  renderWithProviders,
} from '../../../shared/test/helpers'
import { installMockIntersectionObserver } from '../../../shared/test/intersection-observer'

vi.mock('../../../shared/api/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
  },
  getTimelinePath: (cursor?: string | null) =>
    cursor != null ? `/timeline?cursor=${cursor}` : '/timeline',
}))

describe('Timeline continuation UI', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('automatically appends timeline items and shows the end state', async () => {
    const observer = installMockIntersectionObserver()

    vi.mocked(apiClient.get)
      .mockResolvedValueOnce(
        createMockTimeline({
          posts: [createMockPost({ id: 1, body: 'Initial timeline post' })],
          nextCursor: 'cursor-1',
        })
      )
      .mockResolvedValueOnce(
        createMockTimeline({
          posts: [createMockPost({ id: 2, body: 'Older timeline post' })],
          nextCursor: null,
        })
      )

    renderWithProviders(<TimelinePage />, {
      session: mockAuthenticatedSession(),
    })

    await screen.findByText('Initial timeline post')
    const sentinel = await screen.findByTestId('collection-continuation-sentinel')

    await act(async () => {
      observer.trigger(sentinel)
    })

    await waitFor(() => {
      expect(screen.getByText('Older timeline post')).toBeInTheDocument()
    })
    expect(screen.getByTestId('collection-end-state')).toBeInTheDocument()
  })
})
