import { beforeEach, describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { AuthProvider } from '../../../app/providers/AuthProvider'
import { ProfilePage } from '../ProfilePage'
import { apiClient } from '../../../shared/api/client'
import { ApiError } from '../../../shared/api/errors'
import {
  createMockPost,
  createMockProfile,
} from '../../../shared/test/factories'
import type {
  ProfileResponse,
  SessionResponse,
} from '../../../shared/api/contracts'

vi.mock('../../../shared/api/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    patch: vi.fn(),
    putForm: vi.fn(),
    delete: vi.fn(),
  },
}))

function renderProfileEditingPage({
  profile = createMockProfile({
    username: 'bob',
    displayName: 'Bob Tester',
    bio: 'Primary seeded user for Postly e2e flows.',
    isSelf: true,
  }),
  posts = [
    createMockPost({
      id: 1,
      authorUsername: 'bob',
      authorDisplayName: 'Bob Tester',
      canEdit: true,
      canDelete: true,
    }),
  ],
  session = {
    userId: 2,
    username: 'bob',
    displayName: 'Bob Tester',
  } as SessionResponse | null,
}: {
  profile?: ReturnType<typeof createMockProfile>
  posts?: ReturnType<typeof createMockPost>[]
  session?: SessionResponse | null
} = {}) {
  const profileResponse: ProfileResponse = {
    profile,
    posts,
    nextCursor: undefined,
  }

  vi.mocked(apiClient.get).mockResolvedValue(profileResponse)

  return render(
    <MemoryRouter initialEntries={['/u/bob']}>
      <AuthProvider initialSession={session}>
        <Routes>
          <Route path="/u/:username" element={<ProfilePage />} />
          <Route path="/signin" element={<div>Signin page</div>} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>
  )
}

describe('profile editing UI', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('enters inline edit mode with the required profile form hooks', async () => {
    const user = userEvent.setup()
    renderProfileEditingPage()

    await user.click(await screen.findByTestId('profile-edit-button'))

    expect(screen.getByTestId('profile-edit-form')).toBeInTheDocument()
    expect(screen.getByTestId('profile-display-name-input')).toHaveValue(
      'Bob Tester'
    )
    expect(screen.getByTestId('profile-bio-input')).toHaveValue(
      'Primary seeded user for Postly e2e flows.'
    )
    expect(screen.getByTestId('profile-avatar-input')).toHaveAttribute(
      'accept',
      'image/jpeg,image/png'
    )
    expect(screen.getByTestId('profile-save-button')).toBeInTheDocument()
    expect(screen.getByTestId('profile-cancel-button')).toBeInTheDocument()
  })

  it('shows local validation messaging and avoids a save request for invalid drafts', async () => {
    const user = userEvent.setup()
    renderProfileEditingPage()

    await user.click(await screen.findByTestId('profile-edit-button'))

    const displayNameInput = screen.getByTestId(
      'profile-display-name-input'
    )
    await user.clear(displayNameInput)
    await user.type(screen.getByTestId('profile-bio-input'), 'x'.repeat(161))
    await user.click(screen.getByTestId('profile-save-button'))

    await waitFor(() => {
      expect(screen.getByTestId('profile-form-status')).toHaveTextContent(
        'Resolve the highlighted profile fields and try again.'
      )
    })

    expect(apiClient.patch).not.toHaveBeenCalled()
    expect(apiClient.putForm).not.toHaveBeenCalled()
  })

  it('shows a pending save state and updates the rendered profile identity after success', async () => {
    const user = userEvent.setup()

    let resolvePatch: ((value: ProfileResponse) => void) | undefined
    vi.mocked(apiClient.patch).mockImplementation(
      () =>
        new Promise((resolve) => {
          resolvePatch = resolve as (value: ProfileResponse) => void
        })
    )

    renderProfileEditingPage()

    await user.click(await screen.findByTestId('profile-edit-button'))
    await user.clear(screen.getByTestId('profile-display-name-input'))
    await user.type(screen.getByTestId('profile-display-name-input'), 'Bob Updated')
    await user.click(screen.getByTestId('profile-save-button'))

    expect(screen.getByTestId('profile-save-button')).toBeDisabled()
    expect(screen.getByTestId('profile-form-status')).toHaveTextContent(
      'Saving profile…'
    )

    await waitFor(() => {
      expect(apiClient.patch).toHaveBeenCalled()
    })

    if (resolvePatch == null) {
      throw new Error('Expected profile patch promise resolver to be assigned')
    }

    resolvePatch({
      profile: createMockProfile({
        username: 'bob',
        displayName: 'Bob Updated',
        bio: 'Primary seeded user for Postly e2e flows.',
        isSelf: true,
      }),
      posts: [],
      nextCursor: undefined,
    })

    await waitFor(() => {
      expect(screen.getByTestId('profile-display-name')).toHaveTextContent(
        'Bob Updated'
      )
      expect(screen.getByTestId('profile-form-status')).toHaveTextContent(
        'Profile saved.'
      )
      expect(screen.getAllByText('Bob Updated').length).toBeGreaterThan(0)
    })
  })

  it('uses the returned versioned avatar URL for the profile header and visible posts', async () => {
    const user = userEvent.setup()
    const initialProfile = createMockProfile({
      username: 'bob',
      displayName: 'Bob Tester',
      avatarUrl: '/api/profiles/bob/avatar?v=1',
      hasCustomAvatar: true,
      isSelf: true,
    })
    const updatedProfile = createMockProfile({
      username: 'bob',
      displayName: 'Bob Tester',
      avatarUrl: '/api/profiles/bob/avatar?v=2',
      hasCustomAvatar: true,
      isSelf: true,
    })

    vi.mocked(apiClient.get).mockResolvedValue({
      profile: initialProfile,
      posts: [
        createMockPost({
          id: 1,
          authorUsername: 'bob',
          authorDisplayName: 'Bob Tester',
          authorAvatarUrl: '/api/profiles/bob/avatar?v=1',
          canEdit: true,
          canDelete: true,
        }),
      ],
      nextCursor: null,
    })
    vi.mocked(apiClient.putForm).mockResolvedValue({
      profile: updatedProfile,
      posts: [],
      nextCursor: null,
    })

    renderProfileEditingPage({
      profile: initialProfile,
      posts: [
        createMockPost({
          id: 1,
          authorUsername: 'bob',
          authorDisplayName: 'Bob Tester',
          authorAvatarUrl: '/api/profiles/bob/avatar?v=1',
          canEdit: true,
          canDelete: true,
        }),
      ],
    })

    await user.click(await screen.findByTestId('profile-edit-button'))
    await user.upload(
      screen.getByTestId('profile-avatar-input'),
      new File(['avatar'], 'avatar.png', { type: 'image/png' })
    )
    await user.click(screen.getByTestId('profile-save-button'))

    await waitFor(() => {
      expect(apiClient.putForm).toHaveBeenCalled()
    })

    await waitFor(() => {
      expect(screen.getByTestId('profile-avatar-image')).toHaveAttribute(
        'src',
        '/api/profiles/bob/avatar?v=2'
      )
    })

    await waitFor(() => {
      const postAvatar = screen.getByTestId('post-avatar-1')
      expect(within(postAvatar).getByRole('img')).toHaveAttribute(
        'src',
        '/api/profiles/bob/avatar?v=2'
      )
    })
  })

  it('preserves draft inputs and keeps saved identity unchanged after a failed save', async () => {
    const user = userEvent.setup()
    vi.mocked(apiClient.patch).mockRejectedValue(
      new ApiError(
        400,
        'VALIDATION_FAILED',
        'One or more validation errors occurred.',
        undefined,
        {
          displayName: ['Display name must be between 1 and 50 characters after trimming.'],
        }
      )
    )

    renderProfileEditingPage()

    await user.click(await screen.findByTestId('profile-edit-button'))
    await user.clear(screen.getByTestId('profile-display-name-input'))
    await user.type(screen.getByTestId('profile-display-name-input'), '  Bob Draft  ')
    await user.click(screen.getByTestId('profile-save-button'))

    await waitFor(() => {
      expect(screen.getByTestId('profile-form-status')).toHaveTextContent(
        'Display name must be between 1 and 50 characters after trimming.'
      )
    })

    expect(screen.getByTestId('profile-display-name-input')).toHaveValue(
      '  Bob Draft  '
    )
    expect(screen.getByTestId('profile-display-name')).toHaveTextContent(
      'Bob Tester'
    )
  })

  it('falls back to the generated avatar when there is no custom avatar or the image fails', async () => {
    const withFallbackProfile = createMockProfile({
      username: 'bob',
      displayName: 'Bob Tester',
      avatarUrl: null,
      hasCustomAvatar: false,
      isSelf: true,
    })

    const firstRender = renderProfileEditingPage({ profile: withFallbackProfile })

    expect(await screen.findByTestId('profile-avatar-fallback')).toBeVisible()

    firstRender.unmount()
    vi.clearAllMocks()
    renderProfileEditingPage({
      profile: createMockProfile({
        username: 'bob',
        displayName: 'Bob Tester',
        avatarUrl: '/api/profiles/bob/avatar?v=9',
        hasCustomAvatar: true,
        isSelf: true,
      }),
    })

    const avatarImage = await screen.findByTestId('profile-avatar-image')
    fireEvent.error(avatarImage)

    await waitFor(() => {
      expect(screen.getByTestId('profile-avatar-fallback')).toBeVisible()
    })
  })
})
