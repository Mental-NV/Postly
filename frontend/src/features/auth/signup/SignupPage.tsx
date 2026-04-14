import { Link } from 'react-router-dom'
import { useSignupForm } from './useSignupForm'
import { Button } from '../../../shared/components/Button'

export function SignupPage(): React.JSX.Element {
  const { values, errors, isPending, handleChange, handleSubmit } =
    useSignupForm()

  return (
    <div className="auth-page">
      <div className="auth-card">
        <div className="auth-brand">Postly</div>
        <h1 className="auth-title">Create your account</h1>

        <form
          onSubmit={(e) => {
            void handleSubmit(e)
          }}
          data-testid="signup-form"
          className="auth-form"
        >
          {errors.form ? <div
              role="alert"
              className="auth-error-top"
              data-testid="form-error"
            >
              {errors.form}
            </div> : null}

          <div className="auth-field">
            <label htmlFor="username">Username</label>
            <input
              id="username"
              type="text"
              value={values.username}
              onChange={(e) => { handleChange('username', e.target.value); }}
              disabled={isPending}
              data-testid="username-input"
              className={errors.username ? 'invalid' : ''}
            />
            {errors.username ? <div className="auth-field-error" data-testid="username-error">
                {errors.username[0]}
              </div> : null}
          </div>

          <div className="auth-field">
            <label htmlFor="displayName">Display Name</label>
            <input
              id="displayName"
              type="text"
              value={values.displayName}
              onChange={(e) => { handleChange('displayName', e.target.value); }}
              disabled={isPending}
              data-testid="displayName-input"
              className={errors.displayName ? 'invalid' : ''}
            />
            {errors.displayName ? <div className="auth-field-error" data-testid="displayName-error">
                {errors.displayName[0]}
              </div> : null}
          </div>

          <div className="auth-field">
            <label htmlFor="bio">Bio (optional)</label>
            <textarea
              id="bio"
              value={values.bio}
              onChange={(e) => { handleChange('bio', e.target.value); }}
              disabled={isPending}
              data-testid="bio-input"
              rows={3}
              className={errors.bio ? 'invalid' : ''}
            />
            {errors.bio ? <div className="auth-field-error" data-testid="bio-error">
                {errors.bio[0]}
              </div> : null}
          </div>

          <div className="auth-field">
            <label htmlFor="password">Password</label>
            <input
              id="password"
              type="password"
              value={values.password}
              onChange={(e) => { handleChange('password', e.target.value); }}
              disabled={isPending}
              data-testid="password-input"
              className={errors.password ? 'invalid' : ''}
            />
            {errors.password ? <div className="auth-field-error" data-testid="password-error">
                {errors.password[0]}
              </div> : null}
          </div>

          <Button
            type="submit"
            disabled={isPending}
            data-testid="submit-button"
            className="auth-submit-btn"
          >
            {isPending ? 'Signing up...' : 'Sign Up'}
          </Button>
        </form>

        <p className="auth-footer">
          Already have an account? <Link to="/signin">Sign in</Link>
        </p>
      </div>
    </div>
  )
}
