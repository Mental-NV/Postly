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

The layout is **mobile-first** and adapts dynamically to the viewport width.

### 2.1 Breakpoints
- **Mobile:** `< 500px`
- **Tablet:** `500px` to `1000px`
- **Desktop:** `> 1000px`

### 2.2 Layout Behavior by Breakpoint

#### Desktop (> 1000px)
- **Left Column (275px):** Full sidebar navigation with icons and text (Home, Profile, Sign Out). Fixed positioning.
- **Middle Column (600px):** Main feed and content. Bordered left and right (1px).
- **Right Column (350px):** "What's happening" and "Who to follow" sections. Visible.

#### Tablet (500px - 1000px)
- **Left Column (88px):** Narrow sidebar showing **icons only**. Fixed positioning.
- **Middle Column (Max 600px):** Main feed and content. Centered with 1px side borders.
- **Right Column:** Hidden (`display: none`).

#### Mobile (< 500px)
- **Bottom Navigation (64px height):** Fixed at the bottom of the screen. Horizontal layout with icons only.
- **Middle Column (100% width):** Full-width feed. Side borders are removed. Includes `padding-bottom` to prevent content overlap with the navigation bar.
- **Right Column:** Hidden (`display: none`).

---

## 3. Component Inventory

### 3.1 Navigation Icons
- **Library:** `lucide-react`
- **Icons:** `Home` (🏠), `User` (👤), `LogOut` (📤).
- **Behavior:** Text labels are hidden on Mobile and Tablet, shown only on Desktop.

### 3.2 Button Variants
- **Primary:** `Postly Blue` background, white text, bold, rounded-pill.
- **Secondary:** White background, `Postly Blue` border, `Postly Blue` text, rounded-pill.
- **Ghost:** Transparent background, `Secondary Text` color, no border, hover effect.

### 3.3 Avatar System
- **Deterministic Colors:** A palette of 8 colors assigned based on the hash of the username.
- **Rendering:** High-contrast initials centered on the background.
- **Sizes:** 
    - `40x40` (Feed)
    - `48x48` (Composer)
    - `80x80` (Profile Page - Mobile)
    - `134x134` (Profile Page - Desktop)

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
- **Banner:** 
    - **Mobile:** `120px` height.
    - **Desktop:** `200px` height.
    - Gradient banner using the user's deterministic color.
- **Profile Info:** 
    - Avatar overlaps the banner by `40px` on mobile and `67px` on desktop.
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
