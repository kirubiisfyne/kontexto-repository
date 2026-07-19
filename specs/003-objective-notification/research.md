# Phase 0: Research & Technical Decisions

## Transient UI Animation & Timing
- **Decision**: Use a Coroutine-based approach for fading the `CanvasGroup` alpha, utilizing `WaitForSecondsRealtime` and `Time.unscaledDeltaTime`.
- **Rationale**: The project Constitution strictly mandates that UI must support instant pause (`Time.timeScale = 0`). Standard `Update` timers or scaled time coroutines would break. An unscaled Coroutine keeps the script simple, self-contained, and perfectly adheres to the constraint without requiring complex Animator setups for a simple alpha fade.
- **Alternatives considered**: Unity `Animator` component (can be set to Unscaled Time, but requires maintaining an animation controller asset for a simple alpha fade, which is unnecessary overhead). 

## Data Binding (UnityEvents)
- **Decision**: The `ObjectiveNotificationUI` will expose simple methods like `ShowNotification(string message)`. The Task System will emit UnityEvents that pass these formatted strings.
- **Rationale**: Adheres to Constitution Principle I (Isolated Architecture). The UI doesn't know what a Task is; it just knows it received text to display.
