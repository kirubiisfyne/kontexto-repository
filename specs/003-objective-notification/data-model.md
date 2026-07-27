# Phase 1: Data Model & State Transitions

## Entity: `ObjectiveNotificationUI` (MonoBehaviour)

**Fields (Inspector):**
- `CanvasGroup canvasGroup` (For fading)
- `TMP_Text notificationText` (For displaying progress)
- `float displayDuration` (How long it stays visible before fading out)
- `float fadeDuration` (How long the fade transition takes)

**State:**
- `enum UIState { Hidden, FadingIn, Visible, FadingOut }`

**State Transitions:**
1. **Hidden -> FadingIn**: Triggered by `ShowNotification()`. If already FadingIn/Visible, it resets the display timer and updates the text.
2. **FadingIn -> Visible**: Occurs when alpha reaches 1.
3. **Visible -> FadingOut**: Occurs after `displayDuration` elapses.
4. **FadingOut -> Hidden**: Occurs when alpha reaches 0.

**Validations:**
- Must ensure that receiving a new notification while `FadingOut` safely interrupts the fade, snaps back to full opacity or transitions to `FadingIn`, and updates the text cleanly.
