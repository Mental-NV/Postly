import {
  useEffect,
  useEffectEvent,
  useState,
  type Dispatch,
  type SetStateAction,
} from 'react'
import type { ContinuationState } from '../api/contracts'
import { getApiErrorMessage } from '../api/errors'

interface ContinuationPage<TItem> {
  items: TItem[]
  nextCursor: string | null
}

interface ResetContinuationCollectionOptions<TItem> {
  items: TItem[]
  nextCursor: string | null
}

interface UseContinuationCollectionOptions<TItem> {
  getKey: (item: TItem) => number | string
  loadMore: (cursor: string) => Promise<ContinuationPage<TItem>>
  loadMoreErrorMessage: string
}

interface UseContinuationCollectionResult<TItem> {
  items: TItem[]
  nextCursor: string | null
  setItems: Dispatch<SetStateAction<TItem[]>>
  reset: (options: ResetContinuationCollectionOptions<TItem>) => void
  retry: () => Promise<void>
  sentinelRef: (node: HTMLDivElement | null) => void
  status: ContinuationState
  errorMessage: string | null
  isLoadingMore: boolean
  shouldRenderContinuation: boolean
}

export function mergeDistinctCollectionItems<TItem>(
  currentItems: TItem[],
  incomingItems: TItem[],
  getKey: (item: TItem) => number | string
): TItem[] {
  if (incomingItems.length === 0) {
    return currentItems
  }

  const seenKeys = new Set(currentItems.map((item) => getKey(item)))
  const distinctIncomingItems = incomingItems.filter((item) => {
    const key = getKey(item)
    if (seenKeys.has(key)) {
      return false
    }

    seenKeys.add(key)
    return true
  })

  return [...currentItems, ...distinctIncomingItems]
}

export function useContinuationCollection<TItem>({
  getKey,
  loadMore,
  loadMoreErrorMessage,
}: UseContinuationCollectionOptions<TItem>): UseContinuationCollectionResult<TItem> {
  const [items, setItems] = useState<TItem[]>([])
  const [nextCursor, setNextCursor] = useState<string | null>(null)
  const [status, setStatus] = useState<ContinuationState>('idle')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [failedCursor, setFailedCursor] = useState<string | null>(null)
  const [sentinelNode, setSentinelNode] = useState<HTMLDivElement | null>(null)

  // React 19 effect events are intentionally excluded from bootstrap-effect
  // dependency chains at call sites. Treat `reset` and `retry` as special
  // event-like entrypoints, not ordinary callbacks.
  const reset = useEffectEvent(
    ({ items: nextItems, nextCursor: nextCursorValue }: ResetContinuationCollectionOptions<TItem>) => {
      setItems(nextItems)
      setNextCursor(nextCursorValue)
      setErrorMessage(null)
      setFailedCursor(null)

      if (nextItems.length > 0 && nextCursorValue == null) {
        setStatus('exhausted')
        return
      }

      setStatus('idle')
    }
  )

  const requestMore = useEffectEvent(async (cursorOverride?: string) => {
    const cursorToLoad = cursorOverride ?? nextCursor
    if (
      cursorToLoad == null ||
      status === 'loading-more' ||
      (cursorOverride == null && status === 'load-more-error') ||
      items.length === 0
    ) {
      return
    }

    setStatus('loading-more')
    setErrorMessage(null)

    try {
      const page = await loadMore(cursorToLoad)

      setItems((currentItems) =>
        mergeDistinctCollectionItems(currentItems, page.items, getKey)
      )
      setNextCursor(page.nextCursor)
      setFailedCursor(null)
      setStatus(page.nextCursor == null ? 'exhausted' : 'idle')
    } catch (error: unknown) {
      setFailedCursor(cursorToLoad)
      setErrorMessage(getApiErrorMessage(error, loadMoreErrorMessage))
      setStatus('load-more-error')
    }
  })

  useEffect(() => {
    if (sentinelNode == null || items.length === 0) {
      return
    }

    if (typeof IntersectionObserver === 'undefined') {
      return
    }

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((entry) => entry.isIntersecting)) {
          void requestMore()
        }
      },
      {
        rootMargin: '0px 0px 240px 0px',
      }
    )

    observer.observe(sentinelNode)

    return () => {
      observer.disconnect()
    }
  }, [items.length, requestMore, sentinelNode])

  const retry = useEffectEvent(async () => {
    if (failedCursor == null) {
      return
    }

    await requestMore(failedCursor)
  })

  const shouldRenderContinuation =
    items.length > 0 &&
    (nextCursor != null
      || status === 'loading-more'
      || status === 'load-more-error'
      || status === 'exhausted')

  return {
    items,
    nextCursor,
    setItems,
    reset,
    retry,
    sentinelRef: setSentinelNode,
    status,
    errorMessage,
    isLoadingMore: status === 'loading-more',
    shouldRenderContinuation,
  }
}
