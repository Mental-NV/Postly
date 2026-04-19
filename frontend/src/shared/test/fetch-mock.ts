import { vi } from 'vitest'
import { ApiError } from '../api/errors'

export function setupFetchSpy() {
  const fetchSpy = vi.spyOn(globalThis, 'fetch')
  return fetchSpy
}

export function mockFetchResponse(data: any, status = 200) {
  return new Response(JSON.stringify(data), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

export function verifyFetchUrl(fetchSpy: any, expectedUrl: string) {
  const calls = fetchSpy.mock.calls
  const urls = calls.map((call: any) => call[0])
  expect(urls).toContain(expectedUrl)
}

export function mockProblemDetailsResponse(
  {
    status = 500,
    type = 'CONTINUATION_FAILED',
    title = 'Unable to load more content',
    detail = 'Please try again.',
    errors,
  }: {
    status?: number
    type?: string
    title?: string
    detail?: string
    errors?: Record<string, string[]>
  } = {}
) {
  return new Response(
    JSON.stringify({
      type,
      title,
      detail,
      errors,
    }),
    {
      status,
      headers: { 'Content-Type': 'application/problem+json' },
    }
  )
}

export function createOneShotContinuationFailure<T>(
  successFactory: () => T | Promise<T>,
  error: ApiError = new ApiError(
    500,
    'CONTINUATION_FAILED',
    'Unable to load more content',
    'Please try again.'
  )
): () => Promise<T> {
  let hasFailed = false

  return async () => {
    if (!hasFailed) {
      hasFailed = true
      throw error
    }

    return successFactory()
  }
}
