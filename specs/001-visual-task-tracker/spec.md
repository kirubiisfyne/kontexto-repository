# Feature Specification: Visual Task Tracker

**Feature Branch**: `001-visual-task-tracker`

**Created**: 2026-07-10

**Status**: Draft

**Input**: User description: "add a visual task tracker for based on the current level"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Active Tasks for Current Level (Priority: P1)

As a player, I want to see a visual list of my active tasks for the current level so that I know what I need to do to progress.

**Why this priority**: Without knowing what tasks to complete, the player cannot progress in the level. This is the core functionality.

**Independent Test**: Can be fully tested by loading a level with predefined tasks and verifying the UI displays those tasks on screen.

**Acceptance Scenarios**:

1. **Given** the player enters a new level with 2 active tasks, **When** the level finishes loading, **Then** the visual task tracker displays both tasks clearly on screen.
2. **Given** the player is in a level, **When** they pause the game, **Then** the visual task tracker remains visible and responsive (unscaled UI).

---

### User Story 2 - Real-time Task Completion Feedback (Priority: P2)

As a player, I want to see a visual indication when a task is completed so that I know I have successfully finished an objective.

**Why this priority**: Immediate feedback reinforces player actions and confirms progression.

**Independent Test**: Can be fully tested by triggering a task completion event in the Inspector and observing the UI update.

**Acceptance Scenarios**:

1. **Given** the visual task tracker is displaying an active task, **When** the task is completed, **Then** the UI visually crosses out, checks off, or removes the task from the active list.
2. **Given** a task is completed, **When** the UI updates, **Then** the update is clear and distinguishable from active tasks.

### Edge Cases

- What happens when a level has no tasks assigned? (Should the tracker hide itself?)
- How does system handle long task descriptions? (Text wrapping, truncating?)
- What happens when a task is dynamically added during gameplay?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST display a UI element that lists the active tasks for the current level.
- **FR-002**: System MUST visually differentiate between active tasks and completed tasks.
- **FR-003**: System MUST update the task list dynamically when a task's state changes (e.g., from active to completed).
- **FR-004**: System MUST use Unscaled Time for UI animations to support instant pause (`Time.timeScale = 0`).
- **FR-005**: System MUST allow designers to configure the visual task tracker in the Unity Inspector without writing code (e.g., via UnityEvents for showing/hiding).
- **FR-006**: System MUST hide the task tracker if there are no active tasks for the current level.
- **FR-007**: System MUST fetch tasks specific to the current level via the existing `LevelTaskTracker`, avoiding global singletons.

### Key Entities

- **TaskTrackerUI**: The visual component responsible for rendering the task list on screen.
- **TaskItemUI**: The visual representation of a single task within the tracker.
- **LevelTaskTracker**: The existing scene-local manager that holds the source of truth for the current level's tasks. We will extend this or hook into it to broadcast state changes to the UI.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Task UI updates within 100ms of a task state change.
- **SC-002**: The Task Tracker UI successfully renders text for at least 5 simultaneous tasks without breaking layout.
- **SC-003**: Designers can place and configure the TaskTracker prefab in a new scene in under 2 minutes without modifying scripts.
- **SC-004**: The UI remains fully rendered and functional when the game is paused.

## Assumptions

- A system for defining and tracking tasks already exists (or will be adapted); this feature focuses on the *visual* representation of those tasks.
- The UI will be a HUD element overlaying the game view.
- Tasks are scoped per level and do not carry over between different levels.
- Task descriptions will fit within a reasonable text limit (e.g., max 100 characters).
