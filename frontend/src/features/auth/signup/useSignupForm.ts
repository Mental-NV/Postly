import type { FormEvent } from 'react';
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../../../app/providers/AuthProvider'
import { apiClient } from '../../../shared/api/client'
import { isApiError } from '../../../shared/api/errors'
import type { SignupRequest } from '../../../shared/api/contracts'

interface FormErrors {
  username?: string[]
  displayName?: string[]
  bio?: string[]
  password?: string[]
  form?: string
}

export function useSignupForm() {
  const navigate = useNavigate()
  const { signin } = useAuth()
  const [values, setValues] = useState({
    username: '',
    displayName: '',
    bio: '',
    password: '',
  })
  const [errors, setErrors] = useState<FormErrors>({})
  const [isPending, setIsPending] = useState(false)

  const handleChange = (field: keyof typeof values, value: string): void => {
    setValues((prev) => ({ ...prev, [field]: value }))
    // Clear field error when user types
    if (errors[field]) {
      setErrors((prev) => {
        const newErrors = { ...prev }
        delete newErrors[field]
        return newErrors
      })
    }
  }

  const handleSubmit = async (e: FormEvent): Promise<void> => {
    e.preventDefault()

    if (isPending) return

    setIsPending(true)
    setErrors({})

    try {
      const request: SignupRequest = {
        username: values.username,
        displayName: values.displayName,
        bio: values.bio || undefined,
        password: values.password,
      }

      await apiClient.post('/auth/signup', request)

      // Automatically sign in after successful signup
      await signin(values.username, values.password)

      // Success - navigate to home timeline
      void navigate('/', { replace: true })
    } catch (error) {
      if (isApiError(error)) {
        if (error.status === 400 && error.errors) {
          // Validation errors - show field-level errors
          setErrors(error.errors as FormErrors)
        } else if (error.status === 409) {
          // Conflict - username taken
          setErrors({ username: ['Username is already taken.'] })
        } else {
          // Generic error
          setErrors({ form: error.title || 'An error occurred during signup.' })
        }
      } else {
        setErrors({ form: 'An unexpected error occurred.' })
      }

      // Clear password on any error (security best practice)
      setValues((prev) => ({ ...prev, password: '' }))
    } finally {
      setIsPending(false)
    }
  }

  return {
    values,
    errors,
    isPending,
    handleChange,
    handleSubmit,
  }
}
