import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { Composer } from '../posts/composer/Composer'
import { PostEditor } from '../posts/editor/PostEditor'
import { ConfirmDialog } from '../../shared/components/ConfirmDialog'
import { apiClient } from '../../shared/api/client'
import type { PostSummary } from '../../shared/api/contracts'

export function TimelinePage() {
  const [posts, setPosts] = useState<PostSummary[]>([])
  const [nextCursor, setNextCursor] = useState<string | null>(null)
  const [editingPostId, setEditingPostId] = useState<number | null>(null)
  const [deletingPostId, setDeletingPostId] = useState<number | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isLoadingMore, setIsLoadingMore] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    loadTimeline()
  }, [])

  async function loadTimeline() {
    setIsLoading(true)
    setError(null)

    try {
      const data = await apiClient.get<{ posts: PostSummary[], nextCursor?: string }>('/api/timeline')

      setPosts(data.posts)
      setNextCursor(data.nextCursor ?? null)
    } catch (err) {
      setError('Failed to load timeline. Please try again.')
    } finally {
      setIsLoading(false)
    }
  }

  async function loadMorePosts() {
    if (!nextCursor || isLoadingMore) return

    setIsLoadingMore(true)

    try {
      const data = await apiClient.get<{ posts: PostSummary[], nextCursor?: string }>(`/api/timeline?cursor=${nextCursor}`)

      setPosts(prev => [...prev, ...data.posts])
      setNextCursor(data.nextCursor ?? null)
    } catch (err) {
      setError('Failed to load more posts')
    } finally {
      setIsLoadingMore(false)
    }
  }

  async function handlePostCreated() {
    // Reload timeline to show new post
    await loadTimeline()
  }

  function getInitials(displayName: string) {
    return displayName
      .split(' ')
      .map(word => word[0])
      .join('')
      .toUpperCase()
      .slice(0, 2)
  }

  async function handleEdit(postId: number, newBody: string) {
    await apiClient.patch(`/posts/${postId}`, { body: newBody })
    setEditingPostId(null)
    // Update local state
    setPosts(
      posts.map((p) => (p.id === postId ? { ...p, body: newBody, isEdited: true } : p))
    )
  }

  async function handleDelete(postId: number) {
    setIsDeleting(true)
    try {
      await apiClient.delete(`/posts/${postId}`)
      setDeletingPostId(null)
      setPosts(posts.filter((p) => p.id !== postId))
    } finally {
      setIsDeleting(false)
    }
  }

  if (isLoading) {
    return (
      <div className="max-w-2xl mx-auto p-4">
        <div className="text-center py-8">Loading timeline...</div>
      </div>
    )
  }

  return (
    <div className="max-w-2xl mx-auto p-4">
      <h1 className="text-3xl font-bold mb-6">Timeline</h1>

      <Composer onPostCreated={handlePostCreated} />

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 my-4">
          <p className="text-red-800">{error}</p>
          <button
            onClick={loadTimeline}
            className="mt-2 px-4 py-2 bg-red-100 hover:bg-red-200 rounded text-red-800"
          >
            Retry
          </button>
        </div>
      )}

      <div className="space-y-4 mt-6">
        {posts.length === 0 ? (
          <div className="bg-white rounded-lg shadow p-8 text-center text-gray-600">
            <p className="mb-2">Your timeline is empty.</p>
            <p className="text-sm">Create a post or follow other users to see content here.</p>
          </div>
        ) : (
          <>
            {posts.map((post) =>
              editingPostId === post.id ? (
                <PostEditor
                  key={post.id}
                  post={post}
                  onSave={(body) => handleEdit(post.id, body)}
                  onCancel={() => setEditingPostId(null)}
                />
              ) : (
                <div key={post.id} className="bg-white rounded-lg shadow p-4">
                  <div className="flex items-start space-x-3">
                    <Link to={`/u/${post.authorUsername}`}>
                      <div className="w-10 h-10 rounded-full bg-gradient-to-br from-blue-400 to-purple-500 flex items-center justify-center text-white font-bold hover:opacity-80">
                        {getInitials(post.authorDisplayName)}
                      </div>
                    </Link>
                    <div className="flex-1">
                      <div className="flex items-center space-x-2">
                        <Link
                          to={`/u/${post.authorUsername}`}
                          className="font-bold hover:underline"
                        >
                          {post.authorDisplayName}
                        </Link>
                        <Link
                          to={`/u/${post.authorUsername}`}
                          className="text-gray-600 hover:underline"
                        >
                          @{post.authorUsername}
                        </Link>
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
                      {(post.canEdit || post.canDelete) && (
                        <div className="mt-3 flex space-x-2">
                          {post.canEdit && (
                            <button
                              onClick={() => setEditingPostId(post.id)}
                              className="px-3 py-1 text-sm bg-blue-100 hover:bg-blue-200 text-blue-700 rounded"
                            >
                              Edit
                            </button>
                          )}
                          {post.canDelete && (
                            <button
                              onClick={() => setDeletingPostId(post.id)}
                              className="px-3 py-1 text-sm bg-red-100 hover:bg-red-200 text-red-700 rounded"
                            >
                              Delete
                            </button>
                          )}
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              )
            )}

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

      <ConfirmDialog
        isOpen={deletingPostId !== null}
        title="Delete Post"
        message="Are you sure you want to delete this post? This action cannot be undone."
        confirmText="Delete"
        onConfirm={() => deletingPostId && handleDelete(deletingPostId)}
        onCancel={() => setDeletingPostId(null)}
        isPending={isDeleting}
      />
    </div>
  )
}
