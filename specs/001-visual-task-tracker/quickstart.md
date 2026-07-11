# Quickstart: Validation Guide for Visual Task Tracker

## Prerequisites
- Unity Editor 2022+ open with the Kontexto project.
- A test scene configured with a `LevelLoader` and `LevelTaskTracker`.

## Validation Scenarios

### Scenario 1: UI Initialization
1. Open a test scene containing predefined tasks.
2. Ensure the `TaskTrackerUI` prefab is in the scene, and its UnityEvents are hooked up to the `LevelTaskTracker`'s new `onTasksInitialized` and `onTaskCompletedEvent` events.
3. Enter Play Mode.
4. **Expected Outcome**: The UI should instantiate a `TaskItemUI` for each active task in the level. The text should match the `TaskData` descriptions.

### Scenario 2: Task Completion
1. While in Play Mode (from Scenario 1), locate a `HostTaskManager` component on a task giver in the Inspector.
2. Trigger the `onCompleted` event or simulate completing the task in-game.
3. **Expected Outcome**: The `LevelTaskTracker` logs the task completion, and the `TaskTrackerUI` immediately updates the corresponding task's visual state (e.g., cross out text or show checkmark).

### Scenario 3: Pause Menu Compatibility (Unscaled Time)
1. While in Play Mode, set `Time.timeScale = 0` (e.g., via a Pause menu or debug script).
2. Complete a task.
3. **Expected Outcome**: The task completion animation (if any) should play normally despite the game being paused, proving it respects Unscaled Time.
