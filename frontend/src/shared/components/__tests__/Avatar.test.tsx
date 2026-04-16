import { fireEvent, render, screen } from '@testing-library/react'
import { Avatar } from '../Avatar'

describe('Avatar', () => {
  it('renders a full-size circular fallback when no custom avatar exists', () => {
    render(
      <Avatar
        username="bob"
        displayName="Bob Tester"
        fallbackTestId="avatar-fallback"
      />
    )

    const fallback = screen.getByTestId('avatar-fallback')

    expect(fallback).toHaveTextContent('BT')
    expect(fallback).toHaveStyle({
      width: '100%',
      height: '100%',
      borderRadius: 'inherit',
    })
  })

  it('falls back to initials when the custom avatar fails to load', async () => {
    render(
      <Avatar
        username="bob"
        displayName="Bob Tester"
        avatarUrl="/api/profiles/bob/avatar?v=1"
        imageTestId="avatar-image"
        fallbackTestId="avatar-fallback"
      />
    )

    fireEvent.error(screen.getByTestId('avatar-image'))

    expect(await screen.findByTestId('avatar-fallback')).toHaveTextContent(
      'BT'
    )
  })
})
