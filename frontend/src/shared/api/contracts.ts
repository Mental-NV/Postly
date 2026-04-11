export interface UserProfile {
  username: string
  displayName: string
  bio?: string
  followerCount: number
  followingCount: number
  isSelf: boolean
  isFollowedByViewer: boolean
}

export interface PostSummary {
  id: number
  authorUsername: string
  authorDisplayName: string
  body: string
  createdAtUtc: string
  isEdited: boolean
  likeCount: number
  likedByViewer: boolean
  canEdit: boolean
  canDelete: boolean
}

export interface PostInteractionState {
  postId: number
  likeCount: number
  likedByViewer: boolean
}

export interface SignupRequest {
  username: string
  displayName: string
  bio?: string
  password: string
}

export interface SigninRequest {
  username: string
  password: string
}

export interface SessionResponse {
  userId: number
  username: string
  displayName: string
}

export interface TimelineResponse {
  posts: PostSummary[]
  nextCursor?: string
}

export interface ProfileResponse {
  profile: UserProfile
  posts: PostSummary[]
  nextCursor?: string
}

export interface CreatePostRequest {
  body: string
}

export interface UpdatePostRequest {
  body: string
}
