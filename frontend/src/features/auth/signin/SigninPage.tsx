import { useSigninForm } from './useSigninForm'

export function SigninPage() {
  const { values, errors, formError, isPending, handleChange, handleSubmit } = useSigninForm()

  return (
    <div style={{ maxWidth: '400px', margin: '2rem auto', padding: '1rem' }}>
      <h1>Sign In</h1>

      <form onSubmit={handleSubmit}>
        {formError && (
          <div role="alert" style={{ color: 'red', marginBottom: '1rem' }}>
            {formError}
          </div>
        )}

        <div style={{ marginBottom: '1rem' }}>
          <label htmlFor="username">Username</label>
          <input
            id="username"
            name="username"
            type="text"
            value={values.username}
            onChange={handleChange}
            disabled={isPending}
            data-testid="username-input"
            style={{ display: 'block', width: '100%' }}
          />
          {errors.username && (
            <div data-testid="username-error" style={{ color: 'red', fontSize: '0.875rem' }}>
              {errors.username[0]}
            </div>
          )}
        </div>

        <div style={{ marginBottom: '1rem' }}>
          <label htmlFor="password">Password</label>
          <input
            id="password"
            name="password"
            type="password"
            value={values.password}
            onChange={handleChange}
            disabled={isPending}
            data-testid="password-input"
            style={{ display: 'block', width: '100%' }}
          />
          {errors.password && (
            <div data-testid="password-error" style={{ color: 'red', fontSize: '0.875rem' }}>
              {errors.password[0]}
            </div>
          )}
        </div>

        <button type="submit" disabled={isPending} data-testid="submit-button">
          {isPending ? 'Signing in...' : 'Sign In'}
        </button>
      </form>

      <p style={{ marginTop: '1rem' }}>
        Don't have an account? <a href="/signup">Sign up</a>
      </p>
    </div>
  )
}
