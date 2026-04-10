import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { apiClient } from '../../shared/api/client';
import type { UserProfile, PostSummary } from '../../shared/api/contracts';

export function ProfilePage() {
  const { username } = useParams<{ username: string }>();

  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [posts, setPosts] = useState<PostSummary[]>([]);
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingMore, setIsLoadingMore] = useState(false);
  const [isFollowPending, setIsFollowPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadProfile();
  }, [username]);

  const loadProfile = async () => {
    if (!username) return;

    setIsLoading(true);
    setError(null);

    try {
      const data = await apiClient.get<{ profile: UserProfile, posts: PostSummary[], nextCursor?: string }>(`/api/profiles/${username}`);

      setProfile(data.profile);
      setPosts(data.posts);
      setNextCursor(data.nextCursor ?? null);
    } catch (err: any) {
      if (err?.status === 404) {
        setError('User not found');
      } else {
        setError('Failed to load profile. Please try again.');
      }
    } finally {
      setIsLoading(false);
    }
  };

  const loadMorePosts = async () => {
    if (!username || !nextCursor || isLoadingMore) return;

    setIsLoadingMore(true);

    try {
      const data = await apiClient.get<{ profile: UserProfile, posts: PostSummary[], nextCursor?: string }>(`/api/profiles/${username}?cursor=${nextCursor}`);

      setPosts(prev => [...prev, ...data.posts]);
      setNextCursor(data.nextCursor ?? null);
    } catch (err) {
      setError('Failed to load more posts');
    } finally {
      setIsLoadingMore(false);
    }
  };

  const handleFollow = async () => {
    if (!username || !profile || isFollowPending) return;

    setIsFollowPending(true);
    const wasFollowing = profile.isFollowedByViewer;

    // Optimistic update
    setProfile({
      ...profile,
      isFollowedByViewer: true,
      followerCount: profile.followerCount + 1
    });

    try {
      await apiClient.post(`/api/profiles/${username}/follow`);
    } catch (err) {
      // Revert on error
      setProfile({
        ...profile,
        isFollowedByViewer: wasFollowing,
        followerCount: profile.followerCount
      });
      setError('Failed to follow user. Please try again.');
    } finally {
      setIsFollowPending(false);
    }
  };

  const handleUnfollow = async () => {
    if (!username || !profile || isFollowPending) return;

    setIsFollowPending(true);
    const wasFollowing = profile.isFollowedByViewer;

    // Optimistic update
    setProfile({
      ...profile,
      isFollowedByViewer: false,
      followerCount: profile.followerCount - 1
    });

    try {
      await apiClient.delete(`/api/profiles/${username}/follow`);
    } catch (err) {
      // Revert on error
      setProfile({
        ...profile,
        isFollowedByViewer: wasFollowing,
        followerCount: profile.followerCount
      });
      setError('Failed to unfollow user. Please try again.');
    } finally {
      setIsFollowPending(false);
    }
  };

  const getInitials = (displayName: string) => {
    return displayName
      .split(' ')
      .map(word => word[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  };

  if (isLoading) {
    return (
      <div className="max-w-2xl mx-auto p-4">
        <div className="text-center py-8">Loading profile...</div>
      </div>
    );
  }

  if (error && !profile) {
    return (
      <div className="max-w-2xl mx-auto p-4">
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-4">
          <p className="text-red-800">{error}</p>
        </div>
        <button
          onClick={loadProfile}
          className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
        >
          Retry
        </button>
        <Link to="/" className="ml-2 px-4 py-2 bg-gray-200 rounded hover:bg-gray-300">
          Back to Timeline
        </Link>
      </div>
    );
  }

  if (!profile) return null;

  return (
    <div className="max-w-2xl mx-auto p-4">
      {/* Profile Header */}
      <div className="bg-white rounded-lg shadow p-6 mb-6">
        <div className="flex items-start justify-between">
          <div className="flex items-start space-x-4">
            {/* Avatar */}
            <div className="w-20 h-20 rounded-full bg-gradient-to-br from-blue-400 to-purple-500 flex items-center justify-center text-white text-2xl font-bold">
              {getInitials(profile.displayName)}
            </div>

            {/* Profile Info */}
            <div>
              <h1 className="text-2xl font-bold">{profile.displayName}</h1>
              <p className="text-gray-600">@{profile.username}</p>
              {profile.bio && (
                <p className="mt-2 text-gray-800">{profile.bio}</p>
              )}
              <div className="flex space-x-4 mt-3 text-sm">
                <span>
                  <strong>{profile.followingCount}</strong> Following
                </span>
                <span>
                  <strong>{profile.followerCount}</strong> Followers
                </span>
              </div>
            </div>
          </div>

          {/* Follow Button */}
          {!profile.isSelf && (
            <button
              onClick={profile.isFollowedByViewer ? handleUnfollow : handleFollow}
              disabled={isFollowPending}
              className={`px-4 py-2 rounded font-medium ${
                profile.isFollowedByViewer
                  ? 'bg-gray-200 hover:bg-gray-300 text-gray-800'
                  : 'bg-blue-500 hover:bg-blue-600 text-white'
              } disabled:opacity-50 disabled:cursor-not-allowed`}
            >
              {isFollowPending ? 'Loading...' : profile.isFollowedByViewer ? 'Unfollow' : 'Follow'}
            </button>
          )}

          {profile.isSelf && (
            <span className="px-4 py-2 bg-blue-50 text-blue-700 rounded font-medium">
              Your profile
            </span>
          )}
        </div>
      </div>

      {/* Error Message */}
      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-4">
          <p className="text-red-800">{error}</p>
        </div>
      )}

      {/* Posts Section */}
      <div className="space-y-4">
        <h2 className="text-xl font-bold">Posts</h2>

        {posts.length === 0 ? (
          <div className="bg-white rounded-lg shadow p-8 text-center text-gray-600">
            {profile.isSelf ? (
              <>
                <p>You haven't posted yet.</p>
                <Link to="/" className="text-blue-500 hover:underline mt-2 inline-block">
                  Create your first post!
                </Link>
              </>
            ) : (
              <p>{profile.displayName} hasn't posted yet.</p>
            )}
          </div>
        ) : (
          <>
            {posts.map(post => (
              <div key={post.id} className="bg-white rounded-lg shadow p-4">
                <div className="flex items-start space-x-3">
                  <div className="w-10 h-10 rounded-full bg-gradient-to-br from-blue-400 to-purple-500 flex items-center justify-center text-white font-bold">
                    {getInitials(post.authorDisplayName)}
                  </div>
                  <div className="flex-1">
                    <div className="flex items-center space-x-2">
                      <span className="font-bold">{post.authorDisplayName}</span>
                      <span className="text-gray-600">@{post.authorUsername}</span>
                      <span className="text-gray-400">·</span>
                      <span className="text-gray-600 text-sm">
                        {new Date(post.createdAtUtc).toLocaleDateString()}
                      </span>
                      {post.isEdited && (
                        <span className="text-gray-500 text-sm">(edited)</span>
                      )}
                    </div>
                    <p className="mt-2 whitespace-pre-wrap">{post.body}</p>
                    <div className="mt-2 flex items-center space-x-4 text-sm text-gray-600">
                      <span>❤️ {post.likeCount}</span>
                    </div>
                  </div>
                </div>
              </div>
            ))}

            {nextCursor && (
              <button
                onClick={loadMorePosts}
                disabled={isLoadingMore}
                className="w-full py-2 bg-gray-100 hover:bg-gray-200 rounded disabled:opacity-50"
              >
                {isLoadingMore ? 'Loading...' : 'Load more'}
              </button>
            )}
          </>
        )}
      </div>
    </div>
  );
}
