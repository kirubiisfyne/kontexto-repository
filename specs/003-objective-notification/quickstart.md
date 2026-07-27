# Phase 1: Quickstart & Validation Guide

## Setup
1. Open Unity.
2. Instantiate the `ObjectiveNotificationCanvas` prefab in a test scene.
3. Select the prefab to view its Inspector.

## Validation Scenarios

### Scenario 1: Basic Display & Fade
- **Action**: In Play Mode, manually invoke the `ShowNotification("0/4 Books")` method via a test UnityEvent (or a simple UI Button in the test scene).
- **Expected**: The UI fades in, displays "0/4 Books", stays visible for the configured `displayDuration`, and completely fades out.

### Scenario 2: Unscaled Time (Pause Support)
- **Action**: Set `Time.timeScale = 0` in the editor (or pause the game using the game's pause system). Trigger the notification.
- **Expected**: The notification still fades in, stays visible, and fades out normally despite the game being paused.

### Scenario 3: Interruption
- **Action**: Trigger `ShowNotification("1/4 Books")`. While it is visible or fading out, quickly trigger `ShowNotification("2/4 Books")`.
- **Expected**: The UI immediately updates the text to "2/4 Books", resets its visibility timer, and does not flicker or break the fade sequence.
