# Research: Visual Task Tracker

## Unknowns Resolved

### 1. Data Source for Tasks
- **Decision**: Use the existing `LevelTaskTracker` component in `Assets/Master/Scripts/SaveSystem/LevelTaskTracker.cs`.
- **Rationale**: The user suggested it, and upon review, it already aggregates all `HostTaskManager` components for the scene and tracks their progress. This aligns perfectly with the project's requirement to rely on scene-local managers (Constitution Section IV).
- **Alternatives considered**: Creating a new `LevelTaskManager` (rejected as it duplicates state and adds unnecessary complexity).

### 2. UI Hooking Mechanism
- **Decision**: Add UnityEvents to `LevelTaskTracker` (`onTasksInitialized`, `onTaskCompleted`) that the `TaskTrackerUI` can subscribe to via the Inspector.
- **Rationale**: Satisfies the Constitution's mandate for "Designer-Centric Workflow" (Section II) and "Pure & Isolated Architecture" (Section I). The systems will not hard-reference each other.
- **Alternatives considered**: Having the UI directly poll or use `GetComponent` to find the tracker (rejected due to hard-referencing constraints).
