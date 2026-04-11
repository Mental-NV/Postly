# UI/UX Design Specification: Postly "Twitter-Style" Overhaul

## 1. Visual Language

Postly is built for real-time conversation. The design is minimal, prioritizing content and readability.

### 1.1 Typography
- **Primary Font:** `Inter`, `system-ui`, `-apple-system`, `BlinkMacSystemFont`, `Segoe UI`, `Roboto`, `Helvetica`, `Arial`, `sans-serif`.
- **Post Body:** `15px` size, `1.5` line height.
- **Headings:** Bold weight, `19px` size for Page Headings, `15px` for Section Headings.
- **Metadata (Timestamps, Handles):** `14px` size, `rgb(83, 100, 111)` color.

### 1.2 Color Palette
- **Postly Blue (Primary):** `rgb(29, 155, 240)` - Used for primary actions, active states, and branding.
- **Background:** `rgb(255, 255, 255)` - Clean white.
- **Border:** `rgb(239, 243, 244)` - Subtle lines for separation.
- **Primary Text:** `rgb(15, 20, 25)` - High contrast black for content.
- **Secondary Text:** `rgb(83, 100, 111)` - Muted gray for metadata and placeholders.
- **Danger:** `rgb(244, 33, 46)` - Used for delete actions and critical errors.

### 1.3 Spacing & Grid
- **Global Spacing Unit:** `4px` increments.
- **Horizontal Padding:** `16px` on mobile and desktop feed.
- **Vertical Padding:** `12px` between post cards.

---

## 2. Shared Layout Pattern

### 2.1 The 3-Column Shell (Desktop)
- **Left Column (275px):** Navigation (Brand, Home, Profile, Sign Out). Fixed positioning.
- **Middle Column (600px):** Main feed and content. Bordered left and right (1px).
- **Right Column (350px):** "What's happening" and "Who to follow" placeholders.

### 2.2 Mobile Shell
- **Top Bar:** Page title and user avatar.
- **Bottom Bar:** Primary navigation (Home, Profile, Composer FAB).

---

## 3. Component Inventory

### 3.1 Button Variants
- **Primary:** `Postly Blue` background, white text, bold, rounded-pill.
- **Secondary:** White background, `Postly Blue` border, `Postly Blue` text, rounded-pill.
- **Ghost:** Transparent background, `Secondary Text` color, no border, hover effect.

### 3.2 Avatar System
- **Deterministic Colors:** A palette of 8 colors assigned based on the hash of the username.
- **Rendering:** High-contrast initials centered on the background.
- **Sizes:** `40x40` (Feed), `48x48` (Composer), `134x134` (Profile Page).

### 3.3 PostCard (The Atomic Unit)
- **Layout:** Horizontal (Avatar on left, content on right).
- **Action Row:** Bottom-aligned icons for Like (Heart), Edit (Pencil), and Delete (Trash).
- **Interactions:**
    - Clicking the card opens the Permalink (`/posts/:postId`).
    - Clicking the author link opens the Profile (`/u/:username`).

---

## 4. Screen-Specific Designs

### 4.1 FE-03 Home Timeline
- **Sticky Header:** "Home" title with `backdrop-filter: blur(12px)`.
- **Floating Composer:** Textarea that expands as you type. Includes a circular progress indicator for characters.
- **Feed:** "Infinite" style scroll with 1px border-bottom separators.

### 4.2 FE-04 Profile Page
- **Banner:** A fixed-height (200px) gradient banner using the user's deterministic color.
- **Profile Info:** Avatar overlaps the banner by 50%.
- **Bio & Stats:** Clean, vertical stack with prominent Follower/Following counts.
- **Tabs:** "Posts" tab (default) underlined with `Postly Blue`.

### 4.3 FE-01/02 Auth Screens
- **Form Card:** Centered with the Postly Logo.
- **Labels:** Floating labels or high-contrast placeholders.
- **Call-to-Action:** Large, full-width primary button.

---

## 5. State Matrix

| State | Visual Treatment |
| :--- | :--- |
| **Loading** | Skeleton shimmers matching the structure of the content. |
| **Empty** | Centered illustration/text with a clear Primary CTA (e.g., "Write your first post"). |
| **Error** | Inline alert for minor errors; full-page error with retry button for critical failures. |
| **Pending** | Button shows a spinner or reduced opacity; inputs are disabled. |

---

## 6. Accessibility & Motion

- **Contrast:** All text MUST meet WCAG AA (4.5:1) standards.
- **Motion:** Use `cubic-bezier(0, 0, 0.2, 1)` for transitions. Likes should have a subtle scale-up effect (1.2x).
- **Focus States:** Every interactive element MUST have a visible, `Postly Blue` focus ring.
