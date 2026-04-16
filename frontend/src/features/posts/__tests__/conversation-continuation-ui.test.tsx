import { act, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { AuthProvider } from '../../../app/providers/AuthProvider'
import { DirectPostPage } from '../DirectPostPage'
import { apiClient } from '../../../shared/api/client'
import {
  createMockConversation,
  createMockPost,
} from '../../../shared/test/factories'
import { installMockIntersectionObserver } from '../../../shared/test/intersection-observer'
import type { SessionResponse } from '../../../shared/api/contracts'

vi.mock('../../../shared/api/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
  },
  getConversationPath: (postId: string | number) => `/posts/${String(postId)}`,
  getRepliesPath: (postId: string | number, cursor?: string | null) =>
    cursor != null
      ? `/posts/${String(postId)}/replies?cursor=${cursor}`
      : `/posts/${String(postId)}/replies`,
}))

function renderConversationPage() {
  return render(
    <MemoryRouter initialEntries={['/posts/123']}>
      <AuthProvider
        initialSession={
          {
            userId: 2,
            username: 'bob',
            displayName: 'Bob Tester',
          } satisfies SessionResponse
        }
      >
        <Routes>
          <Route path="/posts/:postId" element={<DirectPostPage />} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>
  )
}

describe('Conversation continuation UI', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('automatically appends replies and shows the explicit end state', async () => {
    const observer = installMockIntersectionObserver()

    vi.mocked(apiClient.get)
      .mockResolvedValueOnce(
        createMockConversation({
          replies: [createMockPost({ id: 10, body: 'Initial reply', isReply: true })],
          nextCursor: 'cursor-1',
        })
      )
      .mockResolvedValueOnce({
        replies: [createMockPost({ id: 11, body: 'Older reply', isReply: true })],
        nextCursor: null,
      })

    renderConversationPage()

    await screen.findByText('Initial reply')
    const sentinel = await screen.findByTestId('collection-continuation-sentinel')

    await act(async () => {
      observer.trigger(sentinel)
    })

    await waitFor(() => {
      expect(screen.getByText('Older reply')).toBeInTheDocument()
    })
    expect(screen.getByTestId('collection-end-state')).toBeInTheDocument()
  })
})
