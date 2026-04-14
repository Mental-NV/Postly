import { Link } from 'react-router-dom'
import { useSigninForm } from './useSigninForm'
import { Button } from '../../../shared/components/Button'

export function SigninPage(): React.JSX.Element {
  const { values, errors, formError, isPending, handleChange, handleSubmit } =
    useSigninForm()

  return (
    <div className="auth-page">
      <div className="auth-card">
        <div className="auth-brand">Postly</div>
        <h1 className="auth-title">Sign in to Postly</h1>

        <form
          onSubmit={(e) => {
            void handleSubmit(e)
          }}
          className="auth-form"
        >
          {formError ? <div role="alert" className="auth-error-top">
              {formError}
            </div> : null}

          <div className="auth-field">
            <label htmlFor="username">Username</label>
            <input
              id="username"
              name="username"
              type="text"
              value={values.username}
              onChange={handleChange}
              disabled={isPending}
              data-testid="username-input"
              className={errors.username ? 'invalid' : ''}
            />
            {errors.username ? <div data-testid="username-error" className="auth-field-error">
                {errors.username[0]}
              </div> : null}
          </div>

          <div className="auth-field">
            <label htmlFor="password">Password</label>
            <input
              id="password"
              name="password"
              type="password"
              value={values.password}
              onChange={handleChange}
              disabled={isPending}
              data-testid="password-input"
              className={errors.password ? 'invalid' : ''}
            />
            {errors.password ? <div data-testid="password-error" className="auth-field-error">
                {errors.password[0]}
              </div> : null}
          </div>

          <Button
            type="submit"
            disabled={isPending}
            data-testid="submit-button"
            className="auth-submit-btn"
          >
            {isPending ? 'Signing in...' : 'Sign In'}
          </Button>
        </form>

        <p className="auth-footer">
          Don't have an account? <Link to="/signup">Sign up</Link>
        </p>
      </div>
    </div>
  )
}
