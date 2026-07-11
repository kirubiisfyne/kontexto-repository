# Tasks: Visual Task Tracker

**Input**: Design documents from `/specs/001-visual-task-tracker/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, quickstart.md

**Tests**: Manual testing in Unity Editor via `quickstart.md`.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [x] T001 Create `Assets/Master/Scripts/UI` and `Assets/Master/Prefabs/UI` directories (if they don't exist)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T002 Modify `LevelTaskTracker` in `Assets/Master/Scripts/SaveSystem/LevelTaskTracker.cs` to add UnityEvents (`onTasksInitialized`, `onTaskCompletedEvent`)
- [x] T003 Update `LevelTaskTracker.SpawnAndRestoreTasks` to invoke `onTasksInitialized`
- [x] T004 Update `LevelTaskTracker.OnTaskCompleted` to invoke `onTaskCompletedEvent`

**Checkpoint**: Foundation ready - user story implementation can now begin.

---

## Phase 3: User Story 1 - View Active Tasks for Current Level (Priority: P1) 🎯 MVP

**Goal**: Display a visual list of active tasks for the current level on screen.

**Independent Test**: Load a level with predefined tasks and verify the UI displays those tasks on screen. It must also remain visible when paused.

### Implementation for User Story 1

- [x] T005 [US1] Create `TaskItemUI` script in `Assets/Master/Scripts/UI/TaskItemUI.cs` with `Setup` method for text
- [ ] T006 [US1] Create `TaskItemUI.prefab` in `Assets/Master/Prefabs/UI/TaskItemUI.prefab` and attach `TaskItemUI.cs`
- [x] T007 [US1] Create `TaskTrackerUI` script in `Assets/Master/Scripts/UI/TaskTrackerUI.cs` to manage the list of tasks
- [x] T008 [US1] Implement `InitializeTasks` in `TaskTrackerUI.cs` to instantiate `TaskItemUI` prefabs
- [ ] T009 [US1] Create `TaskTrackerUI.prefab` in `Assets/Master/Prefabs/UI/TaskTrackerUI.prefab` with unscaled time canvas
- [ ] T010 [US1] Place `TaskTrackerUI.prefab` in test scene and wire `onTasksInitialized` from `LevelTaskTracker` to `TaskTrackerUI.InitializeTasks`

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - Real-time Task Completion Feedback (Priority: P2)

**Goal**: Visual indication when a task is completed so that I know I have successfully finished an objective.

**Independent Test**: Triggering a task completion event in the Inspector and observing the UI update (e.g. crossing out the task).

### Implementation for User Story 2

- [x] T011 [US2] Update `TaskItemUI.cs` in `Assets/Master/Scripts/UI/TaskItemUI.cs` to add `MarkCompleted` visual logic (strikethrough or icon)
- [ ] T012 [US2] Update `TaskItemUI.prefab` in `Assets/Master/Prefabs/UI/TaskItemUI.prefab` to support completion visuals (e.g., add checkmark image object)
- [x] T013 [US2] Update `TaskTrackerUI.cs` in `Assets/Master/Scripts/UI/TaskTrackerUI.cs` to handle `OnTaskCompleted` and forward to correct `TaskItemUI`
- [ ] T014 [US2] Wire `onTaskCompletedEvent` from `LevelTaskTracker` to `TaskTrackerUI.OnTaskCompleted` in the test scene

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T015 Run quickstart.md validation manually in Unity Editor to ensure full integration and pause-menu compatibility.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
- **Polish (Final Phase)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Depends on User Story 1 (P1) as it builds on the visual list created in US1

### Within Each User Story

- Scripts before prefabs
- Logic before Inspector wiring
- Story complete before moving to next priority

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Test User Story 1 independently in Play Mode
5. Ready for US2

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Displays active tasks (MVP)
3. Add User Story 2 → Test independently → Tasks cross out when completed
