import { Link } from 'react-router-dom'
import { useSignupForm } from './useSignupForm'

export function SignupPage() {
  const { values, errors, isPending, handleChange, handleSubmit } =
    useSignupForm()

  return (
    <div style={{ maxWidth: '400px', margin: '2rem auto', padding: '1rem' }}>
      <h1>Sign Up</h1>

      <form onSubmit={handleSubmit} data-testid="signup-form">
        {errors.form && (
          <div
            role="alert"
            style={{ color: 'red', marginBottom: '1rem' }}
            data-testid="form-error"
          >
            {errors.form}
          </div>
        )}

        <div style={{ marginBottom: '1rem' }}>
          <label htmlFor="username" style={{ display: 'block' }}>
            Username
          </label>
          <input
            id="username"
            type="text"
            value={values.username}
            onChange={(e) => handleChange('username', e.target.value)}
            disabled={isPending}
            data-testid="username-input"
            style={{ width: '100%', padding: '0.5rem' }}
          />
          {errors.username && (
            <div
              style={{ color: 'red', fontSize: '0.875rem' }}
              data-testid="username-error"
            >
              {errors.username[0]}
            </div>
          )}
        </div>

        <div style={{ marginBottom: '1rem' }}>
          <label htmlFor="displayName" style={{ display: 'block' }}>
            Display Name
          </label>
          <input
            id="displayName"
            type="text"
            value={values.displayName}
            onChange={(e) => handleChange('displayName', e.target.value)}
            disabled={isPending}
            data-testid="displayName-input"
            style={{ width: '100%', padding: '0.5rem' }}
          />
          {errors.displayName && (
            <div
              style={{ color: 'red', fontSize: '0.875rem' }}
              data-testid="displayName-error"
            >
              {errors.displayName[0]}
            </div>
          )}
        </div>

        <div style={{ marginBottom: '1rem' }}>
          <label htmlFor="bio" style={{ display: 'block' }}>
            Bio (optional)
          </label>
          <textarea
            id="bio"
            value={values.bio}
            onChange={(e) => handleChange('bio', e.target.value)}
            disabled={isPending}
            data-testid="bio-input"
            style={{ width: '100%', padding: '0.5rem' }}
            rows={3}
          />
          {errors.bio && (
            <div
              style={{ color: 'red', fontSize: '0.875rem' }}
              data-testid="bio-error"
            >
              {errors.bio[0]}
            </div>
          )}
        </div>

        <div style={{ marginBottom: '1rem' }}>
          <label htmlFor="password" style={{ display: 'block' }}>
            Password
          </label>
          <input
            id="password"
            type="password"
            value={values.password}
            onChange={(e) => handleChange('password', e.target.value)}
            disabled={isPending}
            data-testid="password-input"
            style={{ width: '100%', padding: '0.5rem' }}
          />
          {errors.password && (
            <div
              style={{ color: 'red', fontSize: '0.875rem' }}
              data-testid="password-error"
            >
              {errors.password[0]}
            </div>
          )}
        </div>

        <button
          type="submit"
          disabled={isPending}
          data-testid="submit-button"
          style={{
            width: '100%',
            padding: '0.75rem',
            backgroundColor: isPending ? '#ccc' : '#007bff',
            color: 'white',
            border: 'none',
            cursor: isPending ? 'not-allowed' : 'pointer',
          }}
        >
          {isPending ? 'Signing up...' : 'Sign Up'}
        </button>
      </form>

      <p style={{ marginTop: '1rem', textAlign: 'center' }}>
        Already have an account? <Link to="/signin">Sign in</Link>
      </p>
    </div>
  )
}
