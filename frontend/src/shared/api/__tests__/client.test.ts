import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { apiClient } from '../client'

describe('apiClient URL construction', () => {
  let fetchMock: ReturnType<typeof vi.fn>

  beforeEach(() => {
    fetchMock = vi.fn()
    globalThis.fetch = fetchMock as typeof fetch
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('prepends /api to GET requests', async () => {
    fetchMock.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ data: 'test' }),
    })

    await apiClient.get('/profiles/alice')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/profiles/alice',
      expect.objectContaining({ credentials: 'same-origin' })
    )
  })

  it('prepends /api to POST requests', async () => {
    fetchMock.mockResolvedValueOnce({
      ok: true,
      status: 204,
    })

    await apiClient.post('/profiles/alice/follow')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/profiles/alice/follow',
      expect.objectContaining({ method: 'POST' })
    )
  })

  it('prepends /api to PATCH requests', async () => {
    fetchMock.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ data: 'updated' }),
    })

    await apiClient.patch('/posts/123', { body: 'test' })

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/posts/123',
      expect.objectContaining({ method: 'PATCH' })
    )
  })

  it('prepends /api to DELETE requests', async () => {
    fetchMock.mockResolvedValueOnce({
      ok: true,
      status: 204,
    })

    await apiClient.delete('/posts/123')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/posts/123',
      expect.objectContaining({ method: 'DELETE' })
    )
  })

  it('does not double-prepend /api', async () => {
    fetchMock.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ data: 'test' }),
    })

    // This should NOT result in /api/api/profiles/alice
    await apiClient.get('/profiles/alice')

    const callUrl = fetchMock.mock.calls[0]?.[0]
    expect(callUrl).toBe('/api/profiles/alice')
    expect(callUrl).not.toContain('/api/api/')
  })
})
