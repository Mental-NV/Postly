export interface UserProfile {
  username: string
  displayName: string
  bio?: string
  avatarUrl?: string | null
  hasCustomAvatar: boolean
  followerCount: number
  followingCount: number
  isSelf: boolean
  isFollowedByViewer: boolean
}

export interface PostSummary {
  id: number
  authorUsername?: string | null
  authorDisplayName?: string | null
  authorAvatarUrl?: string | null
  body?: string | null
  createdAtUtc: string
  isEdited: boolean
  likeCount: number
  likedByViewer: boolean
  canEdit: boolean
  canDelete: boolean
  isReply: boolean
  replyToPostId?: number | null
  state: 'available' | 'deleted'
}

export interface ConversationTarget {
  state: 'available' | 'unavailable'
  post?: PostSummary
}

export interface ConversationResponse {
  target: ConversationTarget
  replies: PostSummary[]
  nextCursor?: string | null
}

export interface ReplyPageResponse {
  replies: PostSummary[]
  nextCursor?: string | null
}

export interface PostInteractionState {
  postId: number
  likeCount: number
  likedByViewer: boolean
}

export interface NotificationsResponse {
  notifications: NotificationSummary[]
}

export interface NotificationSummary {
  id: number
  kind: string
  actorUsername: string
  actorDisplayName: string
  createdAtUtc: string
  isRead: boolean
  destinationKind: string
  destinationRoute: string
  destinationState: string
}

export interface NotificationOpenResponse {
  notification: NotificationSummary
  destination: NotificationDestination
}

export interface NotificationDestination {
  state: string
  route: string
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

export interface UpdateProfileRequest {
  displayName: string
  bio: string | null
}

export interface CreatePostRequest {
  body: string
}

export interface UpdatePostRequest {
  body: string
}

export interface CreateReplyRequest {
  body: string
}

export interface PostResponse {
  post: PostSummary
}
