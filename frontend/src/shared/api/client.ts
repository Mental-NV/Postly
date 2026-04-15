import { ApiError } from './errors'

const API_BASE = '/api'

interface ProblemDetails {
  type?: string
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const contentType = response.headers.get('content-type')
    if (contentType?.includes('application/problem+json')) {
      const problem = await response.json() as ProblemDetails
      throw new ApiError(
        response.status,
        problem.type ?? 'UNKNOWN_ERROR',
        problem.title ?? 'An error occurred',
        problem.detail,
        problem.errors
      )
    }
    throw new ApiError(
      response.status,
      'HTTP_ERROR',
      `HTTP ${response.status}`,
      response.statusText
    )
  }

  if (response.status === 204) {
    return undefined as T
  }

  return response.json() as Promise<T>
}

export const apiClient = {
  async get<T>(path: string): Promise<T> {
    const response = await fetch(`${API_BASE}${path}`, {
      credentials: 'same-origin',
    })
    return handleResponse<T>(response)
  },

  async post<T>(path: string, body?: unknown): Promise<T> {
    const response = await fetch(`${API_BASE}${path}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      credentials: 'same-origin',
      body: body ? JSON.stringify(body) : undefined,
    })
    return handleResponse<T>(response)
  },

  async patch<T>(path: string, body?: unknown): Promise<T> {
    const response = await fetch(`${API_BASE}${path}`, {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json',
      },
      credentials: 'same-origin',
      body: body ? JSON.stringify(body) : undefined,
    })
    return handleResponse<T>(response)
  },

  async putForm<T>(path: string, formData: FormData): Promise<T> {
    const response = await fetch(`${API_BASE}${path}`, {
      method: 'PUT',
      credentials: 'same-origin',
      body: formData,
    })
    return handleResponse<T>(response)
  },

  async delete<T>(path: string): Promise<T> {
    const response = await fetch(`${API_BASE}${path}`, {
      method: 'DELETE',
      credentials: 'same-origin',
    })
    return handleResponse<T>(response)
  },
}

export function getConversationPath(postId: number | string, cursor?: string | null): string {
  const params = cursor ? `?cursor=${encodeURIComponent(cursor)}` : ''
  return `/posts/${String(postId)}${params}`
}

export function getRepliesPath(postId: number | string, cursor?: string | null): string {
  const params = cursor ? `?cursor=${encodeURIComponent(cursor)}` : ''
  return `/posts/${String(postId)}/replies${params}`
}
