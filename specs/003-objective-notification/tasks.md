# Tasks: Objective Progress Notification

**Input**: Design documents from `/specs/003-objective-notification/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, quickstart.md

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [x] T001 Create `ObjectiveNotificationUI.cs` file in `Assets/Scripts/UI/ObjectiveNotificationUI.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T002 Define the `UIState` enum and expose inspector variables (`canvasGroup`, `notificationText`, `displayDuration`, `fadeDuration`) in `Assets/Scripts/UI/ObjectiveNotificationUI.cs`
- [ ] T003 Create the `ObjectiveNotificationCanvas.prefab` in `Assets/Prefabs/UI/ObjectiveNotificationCanvas.prefab` and attach the script, CanvasGroup, and TextMeshProUGUI.

**Checkpoint**: Foundation ready - script and prefab are linked.

---

## Phase 3: User Story 1 - Task Activation Notification (Priority: P1) 🎯 MVP

**Goal**: As a player, I want to be notified of my exact goal as soon as a task begins (e.g., "0/4 books").

**Independent Test**: Trigger a task activation event; verify the notification UI appears, displays the initial `0/total` state, and automatically hides after a brief delay.

### Implementation for User Story 1

- [x] T004 [US1] Implement `ShowNotification(string message)` logic in `Assets/Scripts/UI/ObjectiveNotificationUI.cs` to set text and start the unscaled coroutine sequence.
- [x] T005 [US1] Implement the unscaled fade-in coroutine logic in `Assets/Scripts/UI/ObjectiveNotificationUI.cs`.
- [x] T006 [US1] Implement the unscaled display duration and fade-out coroutine logic in `Assets/Scripts/UI/ObjectiveNotificationUI.cs`.

**Checkpoint**: At this point, the UI can successfully show a notification and fade out cleanly.

---

## Phase 4: User Story 2 - Task Progress Notification (Priority: P1)

**Goal**: As a player, I want a transient notification to appear whenever I make progress on a task.

**Independent Test**: Trigger an objective progress update event while a notification is already visible; verify the UI correctly updates the text and resets the display timer without flickering.

### Implementation for User Story 2

- [x] T007 [US2] Add interruption handling in `Assets/Scripts/UI/ObjectiveNotificationUI.cs` so that calling `ShowNotification()` while already `Visible` or `FadingOut` stops the current coroutine, updates the text, resets alpha to 1, and restarts the display timer.

**Checkpoint**: The UI now safely handles rapid, sequential progress updates.

---

## Phase 5: User Story 3 - Task Completion Notification (Priority: P2)

**Goal**: As a player, I want a final notification when I complete the objective, so I know I can move on.

**Independent Test**: Trigger a task completion event; verify the UI shows the completed state (e.g., "4/4" or "Completed"), fades out, and does not break future usages.

### Implementation for User Story 3

- [ ] T008 [US3] Verify completion functionality uses the existing `ShowNotification()` logic without requiring architectural changes, handling the final message flawlessly. (No code changes expected, serves as validation checkpoint).

**Checkpoint**: All user stories should now be independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validation and final cleanup.

- [ ] T009 Run the `quickstart.md` validation scenarios in Unity Play Mode to ensure unscaled time works correctly during a paused game state.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Can start immediately.
- **Foundational (Phase 2)**: Depends on Setup.
- **User Stories (Phase 3+)**: Must be executed sequentially since they build upon the exact same script logic.
- **Polish (Final Phase)**: Depends on all user stories being complete.

### Implementation Strategy

1. Complete Setup + Foundational → Unity Prefab is ready.
2. Complete User Story 1 → Basic fade in/out works. (MVP)
3. Complete User Story 2 → Interruption/rapid-fire works.
4. Polish → Final validation in the engine.
