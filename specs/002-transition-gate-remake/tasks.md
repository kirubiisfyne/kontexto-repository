# Tasks: Transition and Gate System Refactor

**Input**: Design documents from `/specs/002-transition-gate-remake/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [x] T001 Review existing scripts to ensure no conflicts before refactor in `Assets/Master/Scripts/Core/TransitionManager.cs` and `Assets/Master/Scripts/SceneManagement/SceneGateManager.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

- [x] T002 [P] Add `gateId` and `targetGateId` string fields to `SceneGateInstance` in `Assets/Master/Scripts/SceneManagement/SceneGateInstance.cs`
- [x] T003 [P] Add `targetGateId` state tracking to `SceneGateManager` in `Assets/Master/Scripts/SceneManagement/SceneGateManager.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin.

---

## Phase 3: User Story 1 - Robust Animator Transitions (Priority: P1) 🎯 MVP

**Goal**: As a player, I want smooth and reliable fade transitions without stuttering or hanging on black screens.

**Independent Test**: Trigger a transition in an isolated test scene. The fade-in and fade-out should execute flawlessly without skipping due to 1-frame initialization delays.

### Implementation for User Story 1

- [x] T004 [US1] Update `Start` coroutine in `Assets/Master/Scripts/Core/TransitionManager.cs` to wait one frame (`yield return null;`) before polling `GetCurrentAnimatorStateInfo(0).length`.

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently.

---

## Phase 4: User Story 2 - Gate ID Based Spawning (Priority: P1)

**Goal**: As a player, I want to always spawn at the correct door or gate when entering a new scene.

**Independent Test**: Place two gates connecting Scene A and Scene B, and ensure the player spawns at the correct corresponding gate based on ID.

### Implementation for User Story 2

- [x] T005 [P] [US2] Update `TryWarp` method in `Assets/Master/Scripts/SceneManagement/SceneGateInstance.cs` to pass `targetGateId` to `SceneGateManager.StartWarp`.
- [x] T006 [US2] Update `StartWarp` and `WarpRoutine` signatures in `Assets/Master/Scripts/SceneManagement/SceneGateManager.cs` to accept the `targetGateId` parameter.
- [x] T007 [US2] Refactor `SpawnPlayerToGate` in `Assets/Master/Scripts/SceneManagement/SceneGateManager.cs` to find the gate matching the passed `targetGateId` instead of `lastSceneString`.

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently.

---

## Phase 5: User Story 3 - Flexible Scene Loading (Priority: P2)

**Goal**: As a developer, I want to warp players to scenes that don't have Gate Instance prefabs, and I want the loading process to be smooth.

**Independent Test**: Test warping to a scene with an empty Target Gate ID and ensure it loads cleanly without throwing missing prefab errors and without visual pop-in.

### Implementation for User Story 3

- [x] T008 [US3] Update `SpawnPlayerToGate` in `Assets/Master/Scripts/SceneManagement/SceneGateManager.cs` to return immediately if `targetGateId` is null or empty.
- [x] T009 [US3] Refactor `WarpRoutine` in `Assets/Master/Scripts/SceneManagement/SceneGateManager.cs` to use `operation.allowSceneActivation = false` during the load sequence and set it to `true` only after `TransitionManager` fade out completes.

**Checkpoint**: All user stories should now be independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T010 Run validation scenarios from `specs/002-transition-gate-remake/quickstart.md` in Play Mode.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - User stories can proceed sequentially (US1 → US2 → US3) or in parallel.
- **Polish (Final Phase)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P1)**: Can start after Foundational (Phase 2) - No dependencies on US1
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - Integrates with US2's `targetGateId` logic

### Parallel Opportunities

- Foundational tasks T002 and T003 can be executed in parallel.
- US1 (T004) and US2/US3 (T005-T009) can be worked on in parallel since they touch completely different files (`TransitionManager.cs` vs `SceneGateManager.cs`).

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Test User Story 1 independently to ensure transitions don't skip.

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 2 → Test independently → Deploy/Demo
4. Add User Story 3 → Test independently → Deploy/Demo
