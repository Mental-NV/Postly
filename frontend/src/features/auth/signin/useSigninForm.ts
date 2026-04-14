import { useState } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { useAuth } from '../../../app/providers/AuthProvider'
import { isApiError } from '../../../shared/api/errors'

export function useSigninForm() {
  const [values, setValues] = useState({ username: '', password: '' })
  const [errors, setErrors] = useState<Record<string, string[]>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [isPending, setIsPending] = useState(false)
  const { signin } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  function handleChange(e: React.ChangeEvent<HTMLInputElement>): void {
    const { name, value } = e.target
    setValues((prev) => ({ ...prev, [name]: value }))
    // Clear field error when user types
    if (errors[name]) {
      setErrors((prev) => {
        const next = { ...prev }
        delete next[name]
        return next
      })
    }
    setFormError(null)
  }

  async function handleSubmit(e: React.FormEvent): Promise<void> {
    e.preventDefault()
    setIsPending(true)
    setErrors({})
    setFormError(null)

    try {
      await signin(values.username, values.password)

      // Get return URL from query params or default to home
      const params = new URLSearchParams(location.search)
      const returnUrl = params.get('returnUrl') || '/'
      void navigate(returnUrl, { replace: true })
    } catch (error) {
      // Clear password on error (security)
      setValues((prev) => ({ ...prev, password: '' }))

      if (isApiError(error)) {
        if (error.status === 400 && error.errors) {
          // Field-level validation errors
          setErrors(error.errors)
        } else if (error.status === 401) {
          // Generic error for invalid credentials
          setFormError('Invalid username or password')
        } else {
          setFormError(error.detail || 'An error occurred. Please try again.')
        }
      } else {
        setFormError('An unexpected error occurred. Please try again.')
      }
    } finally {
      setIsPending(false)
    }
  }

  return {
    values,
    errors,
    formError,
    isPending,
    handleChange,
    handleSubmit,
  }
}
