export class ApiError extends Error {
  constructor(
    public status: number,
    public type: string,
    public title: string,
    public detail?: string,
    public errors?: Record<string, string[]>
  ) {
    super(title)
    this.name = 'ApiError'
  }
}

export function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError
}

export function getFieldErrors(
  error: unknown,
  field: string
): string[] {
  if (!isApiError(error)) {
    return []
  }

  return error.errors?.[field] ?? []
}

export function getFormErrorMessage(error: unknown): string | null {
  if (!isApiError(error)) {
    return null
  }

  if (error.detail) {
    return error.detail
  }

  const firstErrorEntry = Object.values(error.errors ?? {})[0]
  return firstErrorEntry?.[0] ?? error.title
}

export function getApiErrorMessage(
  error: unknown,
  fallbackMessage: string
): string {
  if (!isApiError(error)) {
    return fallbackMessage
  }

  if (error.detail != null && error.detail.length > 0) {
    return error.detail
  }

  if (error.title.length > 0) {
    return error.title
  }

  return fallbackMessage
}
