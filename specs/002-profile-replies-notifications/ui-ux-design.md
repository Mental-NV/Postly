# UI/UX Design Specification: Postly Round 2 Social Extensions

## 1. Visual Language & Consistency

Round 2 extends the minimal, content-first visual language established in the MVP. It introduces new interactive states and navigational elements while maintaining the existing typography, color palette, and spacing units.

### 1.1 New Visual Elements
- **Icon Style:** All icons MUST be minimalistic monochrome.
    - **Foreground:** `rgb(15, 20, 25)` (Primary Text/Black).
    - **Background:** Transparent.
    - **Stroke Width:** `2px` (standard Lucide weight).
- **Notification Badge:** Small `Postly Blue` dot or counter, positioned top-right of the Notification icon.
- **Unread State:** Notifications that are unread have a subtle light-blue background (`rgba(29, 155, 240, 0.05)`) and a vertical blue bar on the left edge.
- **Thread Lines:** Thin vertical lines (`rgb(207, 217, 222)`) connecting avatars in a conversation to indicate parent-child relationships.
- **Placeholder Text:** `rgb(83, 100, 111)` - Used for "Reply unavailable" or "Parent post unavailable".

---

## 2. Breakpoints & Shared Layout

Breakpoints remain consistent with MVP.

- **Mobile:** `< 500px`
- **Tablet:** `500px` to `1000px`
- **Desktop:** `> 1000px`

### 2.1 Navigation Update
- **Notifications Link:** Added to the sidebar (Desktop/Tablet) and Bottom Navigation (Mobile).
- **Icon:** `Bell` from `lucide-react`.

---

## 3. Component Patterns

### 3.1 Profile Edit State (Inline)
Instead of a separate settings page, editing occurs inline on the `/u/:username` route to maintain context.
- **Transition:** Clicking "Edit Profile" swaps read-only fields for inputs.
- **Inputs:** 
    - **Display Name:** Full-width text input, bold.
    - **Bio:** Multi-line textarea with character count (0/160) at bottom-right.
    - **Avatar:** Overlay on the current avatar with a "Camera" icon and file picker.
- **Actions:** "Save" (Primary Blue) and "Cancel" (Secondary Ghost) buttons fixed at the top-right of the profile header area.

### 3.2 Conversation View (Threading)
The conversation view (`/posts/:postId`) emphasizes the hierarchy of replies.
- **Target Post:** Large-format card at the top.
- **Replies:** Standard post cards, but with a connecting thread line if they are direct children.
- **Indentation:** Replies are NOT indented (to save horizontal space on mobile), but the thread line visually links the avatars.
- **Composer:** A "Reply to [username]" input area fixed at the bottom (Mobile) or below the target post (Desktop/Tablet).

### 3.3 Notifications List
A dedicated list for activity.
- **Header:** "Notifications" with sticky blur effect.
- **Item Layout:** 
    - **Monochrome Icon:** Minimalistic icon indicating activity type (Heart for like, User for follow, MessageCircle for reply). 
    - **Actor Avatar:** Circular avatar of the triggering user.
    - **Notification Text:** "[User] liked your post", "[User] followed you", etc.
    - **Timestamp:** Muted secondary text.
- **Interaction:** Clicking the entire row navigates to the destination.

### 3.4 Continuation Loading Surfaces (Infinite Scroll)
Consistent behavior across all long lists.
- **Sentinel:** An invisible element at the bottom of the list that triggers the next page load.
- **Loading State:** 3-5 skeleton shimmers matching the `PostCard` layout.
- **Error State:** A full-width banner at the bottom: "Couldn't load more posts. [Retry Button]".
- **End State:** A centered, muted message: "You've reached the end of the road." or "No more posts to show."

---

## 4. Screen Composition

### 4.1 FE-07 Profile (Edit State)
- The banner remains.
- The avatar image gets a semi-transparent dark overlay with an "Upload" icon.
- Display Name and Bio transform into bordered input fields.
- Stats (Followers/Following) are moved slightly down or dimmed during edit.

### 4.2 FE-08 Conversation View
- **Header:** "Post" or "Conversation".
- **Main Column:**
    - [Parent Post Placeholder (if unavailable)]
    - [Target Post Card]
    - [Reply Composer]
    - [List of Reply Cards with thread lines]

### 4.3 FE-09 Notifications List
- **Header:** "Notifications".
- **Empty State:** Centered "Bell" icon with text "No notifications yet. When people interact with you, you'll see it here."

---

## 5. State Matrix (Extensions)

| State | Visual Treatment |
| :--- | :--- |
| **Inline Validation Error** | Red border on input + `Danger` text label below. |
| **Unread Notification** | Light blue tint + left-accent border. |
| **Deleted Reply** | Muted box with "This reply was deleted by the author." No actions. |
| **Unavailable Parent** | "This post is no longer available." styled as a ghost post card. |
| **Continuation Loading** | Pulsing skeleton shimmers at the bottom of the feed. |

---

## 6. Motion & Feedback

- **Navigation:** Slide-in transitions between main routes.
- **Edit Toggle:** Cross-fade between read and edit states (200ms).
- **Notification Entry:** New notifications (if updated in real-time) should slide down from the top of the list.
