import { useEffect, useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { apiClient } from '../../shared/api/client';
import type { PostSummary } from '../../shared/api/contracts';
import { PostEditor } from './editor/PostEditor';
import { ConfirmDialog } from '../../shared/components/ConfirmDialog';

export function DirectPostPage() {
  const { postId } = useParams<{ postId: string }>();
  const navigate = useNavigate();

  const [post, setPost] = useState<PostSummary | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [editingPostId, setEditingPostId] = useState<number | null>(null);
  const [deletingPostId, setDeletingPostId] = useState<number | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  useEffect(() => {
    loadPost();
  }, [postId]);

  const loadPost = async () => {
    if (!postId) return;

    setIsLoading(true);
    setError(null);
    setNotFound(false);

    try {
      const data = await apiClient.get<PostSummary>(`/api/posts/${postId}`);
      setPost(data);
    } catch (err: any) {
      if (err?.status === 404) {
        setNotFound(true);
      } else {
        setError('Failed to load post. Please try again.');
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handleEdit = async (postId: number, newBody: string) => {
    await apiClient.patch(`/api/posts/${postId}`, { body: newBody });
    setEditingPostId(null);
    if (post) {
      setPost({ ...post, body: newBody, isEdited: true });
    }
  };

  const handleDelete = async (postId: number) => {
    setIsDeleting(true);
    try {
      await apiClient.delete(`/api/posts/${postId}`);
      navigate('/');
    } finally {
      setIsDeleting(false);
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
        <div className="text-center py-8">Loading post...</div>
      </div>
    );
  }

  if (notFound) {
    return (
      <div className="max-w-2xl mx-auto p-4">
        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-8 text-center">
          <h2 className="text-xl font-bold mb-2">Post not available</h2>
          <p className="text-gray-600 mb-4">This post may have been deleted or does not exist.</p>
          <Link
            to="/"
            className="inline-block px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
          >
            Back to Timeline
          </Link>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="max-w-2xl mx-auto p-4">
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-4">
          <p className="text-red-800">{error}</p>
        </div>
        <button
          onClick={loadPost}
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

  if (!post) return null;

  return (
    <div className="max-w-2xl mx-auto p-4">
      <Link to="/" className="inline-flex items-center text-blue-500 hover:underline mb-4">
        ← Back to Timeline
      </Link>

      {editingPostId === post.id ? (
        <PostEditor
          post={post}
          onSave={(body) => handleEdit(post.id, body)}
          onCancel={() => setEditingPostId(null)}
        />
      ) : (
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-start space-x-4">
            <Link to={`/u/${post.authorUsername}`}>
              <div className="w-12 h-12 rounded-full bg-gradient-to-br from-blue-400 to-purple-500 flex items-center justify-center text-white font-bold hover:opacity-80">
                {getInitials(post.authorDisplayName)}
              </div>
            </Link>
            <div className="flex-1">
              <div className="flex items-center space-x-2">
                <Link
                  to={`/u/${post.authorUsername}`}
                  className="font-bold text-lg hover:underline"
                >
                  {post.authorDisplayName}
                </Link>
                <Link
                  to={`/u/${post.authorUsername}`}
                  className="text-gray-600 hover:underline"
                >
                  @{post.authorUsername}
                </Link>
              </div>
              <p className="text-gray-600 text-sm mt-1">
                {new Date(post.createdAtUtc).toLocaleString()}
                {post.isEdited && <span className="ml-2">(edited)</span>}
              </p>
              <p className="mt-4 text-lg whitespace-pre-wrap">{post.body}</p>
              <div className="mt-4 flex items-center space-x-4 text-gray-600">
                <span>❤️ {post.likeCount}</span>
              </div>
              {(post.canEdit || post.canDelete) && (
                <div className="mt-4 flex space-x-2">
                  {post.canEdit && (
                    <button
                      onClick={() => setEditingPostId(post.id)}
                      className="px-4 py-2 bg-blue-100 hover:bg-blue-200 text-blue-700 rounded"
                    >
                      Edit
                    </button>
                  )}
                  {post.canDelete && (
                    <button
                      onClick={() => setDeletingPostId(post.id)}
                      className="px-4 py-2 bg-red-100 hover:bg-red-200 text-red-700 rounded"
                    >
                      Delete
                    </button>
                  )}
                </div>
              )}
            </div>
          </div>
        </div>
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
  );
}
