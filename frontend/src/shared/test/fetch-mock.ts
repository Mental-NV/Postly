import { vi } from 'vitest'

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
