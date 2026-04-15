import { useEffect, useMemo, useState } from 'react'

interface AvatarProps {
  username: string
  displayName: string
  avatarUrl?: string | null
  size?: 'sm' | 'md' | 'lg'
  className?: string
  wrapperTestId?: string
  imageTestId?: string
  fallbackTestId?: string
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
  avatarUrl = null,
  size = 'md',
  className = '',
  wrapperTestId,
  imageTestId,
  fallbackTestId,
}: AvatarProps): React.JSX.Element {
  const [hasImageError, setHasImageError] = useState(false)

  useEffect(() => {
    setHasImageError(false)
  }, [avatarUrl])

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
  const showImage = avatarUrl != null && !hasImageError

  return (
    <div
      className={classes}
      aria-label={displayName}
      data-testid={wrapperTestId}
    >
      {showImage ? (
        <img
          src={avatarUrl}
          alt={displayName}
          className="avatar-image"
          data-testid={imageTestId}
          onError={() => {
            setHasImageError(true)
          }}
        />
      ) : (
        <div
          style={{ backgroundColor }}
          className="avatar-fallback"
          data-testid={fallbackTestId}
          aria-hidden="true"
        >
          {initials}
        </div>
      )}
    </div>
  )
}
