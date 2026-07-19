# Feature Specification: Objective Progress Notification

**Feature Branch**: `[003-objective-notification]`

**Created**: 2026-07-19

**Status**: Draft

**Input**: User description: "A transient notification UI that shows objective progress (e.g., 1/4) when a task is activated, when progress is made, and when completed. It fades out after a few seconds to avoid screen clutter."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Task Activation Notification (Priority: P1)

As a player, I want to be notified of my exact goal as soon as a task begins (e.g., "0/4 books") so that I immediately understand what I need to do.

**Why this priority**: Establishes the player's goal immediately upon receiving the task.

**Independent Test**: Trigger a task activation event; verify the notification UI appears, displays the initial `0/total` state, and automatically hides after a brief delay.

**Acceptance Scenarios**:

1. **Given** no active notifications, **When** a task is activated, **Then** the notification UI appears showing the baseline progress (e.g., "0/4").
2. **Given** the notification UI is visible, **When** a few seconds pass, **Then** the UI automatically fades out.

---

### User Story 2 - Task Progress Notification (Priority: P1)

As a player, I want a transient notification to appear whenever I make progress on a task, so that I know my actions are being recorded without my screen getting cluttered.

**Why this priority**: Core feedback loop for the player to understand they are successfully advancing their goal.

**Independent Test**: Trigger an objective progress update event; verify the notification UI appears with the new count, and then fades out.

**Acceptance Scenarios**:

1. **Given** an active task, **When** progress is made, **Then** the notification UI appears showing the updated count (e.g., "1/4").
2. **Given** the notification UI is visible after a progress update, **When** a few seconds pass, **Then** it automatically fades out.

---

### User Story 3 - Task Completion Notification (Priority: P2)

As a player, I want a final notification when I complete the objective, so I know I can move on.

**Why this priority**: Closes the gameplay loop for that specific objective.

**Independent Test**: Trigger a task completion event; verify the UI shows the completed state, fades out, and does not trigger again for that task.

**Acceptance Scenarios**:

1. **Given** a task at its final step, **When** the final progress is made, **Then** the UI appears showing completion (e.g., "4/4" or "Completed").
2. **Given** the UI showed completion, **When** it fades out, **Then** it will no longer trigger for this specific task.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST display a notification UI when a task is activated, progressed, or completed.
- **FR-002**: System MUST format the progress clearly (e.g., `[current] / [total]`).
- **FR-003**: System MUST automatically hide/fade out the notification UI after a configurable delay (e.g., 3 seconds).
- **FR-004**: The UI logic MUST be driven by Inspector-wired UnityEvents to decouple it from core task progression logic (per Constitution Principles I & II).

### Key Entities

- **ObjectiveNotificationUI**: Manages the transient display, formatting, and auto-hide timing of the progress notification.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Players see visual progress feedback immediately upon objective activation and progression.
- **SC-002**: The UI successfully clears itself from the screen after the set delay, ensuring zero permanent screen clutter.
- **SC-003**: The UI system integrates cleanly via UnityEvents without requiring hard code references to the Task system.

## Assumptions

- The game already has an underlying task/objective system capable of emitting events for activation, progress, and completion.
- Animations (fade in/out) will be handled via Unity's animation system or simple coroutines/tweens on the UI side.
