import { useMemo } from 'react'

interface AvatarProps {
  username: string
  displayName: string
  size?: 'sm' | 'md' | 'lg'
  className?: string
}

const COLORS = [
  '#1D9BF0', // Postly Blue
  '#7856FF', // Purple
  '#FF7A00', // Orange
  '#00BA7C', // Green
  '#F91880', // Pink
  '#FFD700', // Yellow
  '#8E44AD', // Amethyst
  '#E74C3C', // Red
]

export function Avatar({
  username,
  displayName,
  size = 'md',
  className = '',
}: AvatarProps): React.JSX.Element {
  const backgroundColor = useMemo(() => {
    let hash = 0
    for (let i = 0; i < username.length; i++) {
      hash = username.charCodeAt(i) + ((hash << 5) - hash)
    }
    return COLORS[Math.abs(hash) % COLORS.length]
  }, [username])

  const initials = useMemo(() => {
    return displayName
      .split(' ')
      .map((n) => n[0])
      .join('')
      .substring(0, 2)
      .toUpperCase()
  }, [displayName])

  const sizeClasses = {
    sm: 'avatar-sm',
    md: 'avatar-md',
    lg: 'avatar-lg',
  }

  const classes = `avatar ${sizeClasses[size]} ${className}`.trim()

  return (
    <div
      className={classes}
      style={{ backgroundColor }}
      aria-label={displayName}
    >
      {initials}
    </div>
  )
}
