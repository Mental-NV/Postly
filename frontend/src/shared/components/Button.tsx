import type { ButtonHTMLAttributes, ReactNode } from 'react'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'ghost'
  children: ReactNode
}

export function Button({
  variant = 'primary',
  children,
  className = '',
  ...props
}: ButtonProps): React.JSX.Element {
  const baseStyles = 'btn'
  const variantStyles = {
    primary: 'btn-primary',
    secondary: 'btn-secondary',
    ghost: 'btn-ghost',
  }

  const classes = `${baseStyles} ${variantStyles[variant]} ${className}`.trim()

  return (
    <button className={classes} {...props}>
      {children}
    </button>
  )
}
