import { useState } from 'react'
import { Composer } from '../posts/composer/Composer'
import { PostEditor } from '../posts/editor/PostEditor'
import { ConfirmDialog } from '../../shared/components/ConfirmDialog'
import { apiClient } from '../../shared/api/client'

interface PostSummary {
  id: number
  authorUsername: string
  authorDisplayName: string
  body: string
  createdAtUtc: string
  isEdited: boolean
  editedAtUtc?: string
  likeCount: number
  likedByViewer: boolean
  canEdit: boolean
  canDelete: boolean
}

export function TimelinePage() {
  const [posts, setPosts] = useState<PostSummary[]>([])
  const [editingPostId, setEditingPostId] = useState<number | null>(null)
  const [deletingPostId, setDeletingPostId] = useState<number | null>(null)
  const [isDeleting, setIsDeleting] = useState(false)

  // For MVP, we'll just track posts created in this session
  // Full timeline loading comes in US4

  async function handlePostCreated() {
    // For now, just show a success message
    // In US4, we'll reload the timeline
    alert('Post created successfully!')
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

  return (
    <div>
      <h1>Timeline</h1>

      <Composer onPostCreated={handlePostCreated} />

      {posts.length === 0 ? (
        <div>No posts yet. Create your first post!</div>
      ) : (
        posts.map((post) =>
          editingPostId === post.id ? (
            <PostEditor
              key={post.id}
              post={post}
              onSave={(body) => handleEdit(post.id, body)}
              onCancel={() => setEditingPostId(null)}
            />
          ) : (
            <div key={post.id}>
              <p>{post.body}</p>
              {post.canEdit && (
                <button onClick={() => setEditingPostId(post.id)}>Edit</button>
              )}
              {post.canDelete && (
                <button onClick={() => setDeletingPostId(post.id)}>Delete</button>
              )}
            </div>
          )
        )
      )}

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
