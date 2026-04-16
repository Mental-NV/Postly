import { act, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useEffect } from 'react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { PostSummary } from '../api/contracts'
import {
  ContinuationEndState,
  ContinuationErrorState,
  ContinuationLoadingState,
} from '../components/LoadingState'
import { useContinuationCollection } from '../hooks/useContinuationCollection'
import { installMockIntersectionObserver } from './intersection-observer'
import { createMockPost } from './factories'
import { createOneShotContinuationFailure } from './fetch-mock'

function HookHarness({
  loadMore,
}: {
  loadMore: (cursor: string) => Promise<{
    items: PostSummary[]
    nextCursor: string | null
  }>
}): React.JSX.Element {
  const {
    items,
    reset,
    retry,
    sentinelRef,
    status,
    errorMessage,
    shouldRenderContinuation,
  } = useContinuationCollection<PostSummary>({
    getKey: (post) => post.id,
    loadMore,
    loadMoreErrorMessage: 'Failed to load more posts. Please try again.',
  })

  useEffect(() => {
    reset({
      items: [createMockPost({ id: 1, body: 'Initial visible post' })],
      nextCursor: 'cursor-1',
    })
  }, [])

  return (
    <div>
      <ul>
        {items.map((post) => (
          <li key={post.id}>{post.body}</li>
        ))}
      </ul>
      {shouldRenderContinuation ? (
        <>
          <div
            data-testid="collection-continuation-sentinel"
            ref={sentinelRef}
          />
          {status === 'loading-more' ? <ContinuationLoadingState /> : null}
          {status === 'load-more-error' && errorMessage != null ? (
            <ContinuationErrorState
              message={errorMessage}
              onRetry={() => {
                void retry()
              }}
            />
          ) : null}
          {status === 'exhausted' ? <ContinuationEndState /> : null}
        </>
      ) : null}
    </div>
  )
}

describe('useContinuationCollection', () => {
  let observer: ReturnType<typeof installMockIntersectionObserver>

  beforeEach(() => {
    observer = installMockIntersectionObserver()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('preserves visible items on a failed continuation and retries with the same cursor', async () => {
    const user = userEvent.setup()

    const loadMore = vi.fn(
      createOneShotContinuationFailure(async () => ({
        items: [createMockPost({ id: 2, body: 'Loaded after retry' })],
        nextCursor: null,
      }))
    )

    render(<HookHarness loadMore={loadMore} />)

    await screen.findByText('Initial visible post')
    const sentinel = await screen.findByTestId('collection-continuation-sentinel')

    await act(async () => {
      observer.trigger(sentinel)
    })

    await waitFor(() => {
      expect(
        screen.getByTestId('collection-continuation-error')
      ).toBeInTheDocument()
    })
    expect(screen.getByText('Initial visible post')).toBeInTheDocument()

    await user.click(screen.getByTestId('collection-continuation-retry'))

    await waitFor(() => {
      expect(screen.getByText('Loaded after retry')).toBeInTheDocument()
    })
    expect(screen.getByText('Initial visible post')).toBeInTheDocument()
    expect(screen.getByTestId('collection-end-state')).toBeInTheDocument()
    expect(loadMore).toHaveBeenNthCalledWith(1, 'cursor-1')
    expect(loadMore).toHaveBeenNthCalledWith(2, 'cursor-1')
  })
})
