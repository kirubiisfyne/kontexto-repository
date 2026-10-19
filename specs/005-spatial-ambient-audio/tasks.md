# Tasks: Spatial Ambient Audio Volume

**Input**: Design documents from `specs/005-spatial-ambient-audio/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `quickstart.md`
**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)

---

## Phase 1: Setup

**Purpose**: Verify audio system dependencies and asset locations

- [X] T001 Verify project structure and AudioMixer channels in `Assets/_Project/Audio/am_Main.mixer`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure required before user stories can execute

- [X] T002 Add `AudioChannel` enum and `GetChannelGroup(AudioChannel channel)` helper method in `Assets/_Project/Scripts/Core/AudioManager.cs`
- [X] T003 Create `SpatialAmbientVolume.cs` class scaffold in `Assets/_Project/Scripts/Core/SpatialAmbientVolume.cs`

**Checkpoint**: Foundation ready - user story implementation can begin.

---

## Phase 3: User Story 1 - Randomized In-Volume Spatial Audio Emission (Priority: P1) 🎯 MVP

**Goal**: Periodically sample a random 3D position inside a parent volume (`BoxCollider`, `SphereCollider`, or fallback bounds) and emit spatial 3D audio without consecutive repeats.

**Independent Test**: Attach component to a GameObject with a `BoxCollider`, assign audio clips, enter Play Mode, and observe audio emitting from varying 3D points inside the collider with 3D attenuation.

- [X] T004 [US1] Implement volume collider point sampling logic (`BoxCollider`, `SphereCollider`, and fallback local bounds) in `Assets/_Project/Scripts/Core/SpatialAmbientVolume.cs`
- [X] T005 [US1] Implement dedicated 3D `AudioSource` child emitter initialization and spatial positioning in `Assets/_Project/Scripts/Core/SpatialAmbientVolume.cs`
- [X] T006 [US1] Implement randomized interval scheduler and audio clip selection without immediate repeats in `Assets/_Project/Scripts/Core/SpatialAmbientVolume.cs`

**Checkpoint**: User Story 1 MVP fully functional and testable independently.

---

## Phase 4: User Story 2 - Seamless AudioManager & AudioMixer Integration (Priority: P1)

**Goal**: Route spatial playback through `AudioManager`'s AudioMixer channels (`BGMVolume` or `SFXVolume`) while supporting fallback in isolated test scenes and optional global clip lookup.

**Independent Test**: Adjust `MasterVolume` and `BGMVolume` / `SFXVolume` sliders; verify that ambient audio scales and mutes accordingly. Verify playback in a scene without `AudioManager` runs without exceptions.

- [X] T007 [US2] Implement dynamic AudioMixer group assignment via `AudioManager.Instance.GetChannelGroup()` with graceful standalone fallback in `Assets/_Project/Scripts/Core/SpatialAmbientVolume.cs`
- [X] T008 [US2] Implement optional clip lookup by name from `AudioManager.sfxSounds` and `AudioManager.bgmSounds` in `Assets/_Project/Scripts/Core/SpatialAmbientVolume.cs`

**Checkpoint**: Ambient audio obeys global volume sliders, mute gates, and isolated scene fallbacks.

---

## Phase 5: User Story 3 - Designer-Centric Playback Control & Volume Triggers (Priority: P2)

**Goal**: Expose parameterless public methods for `UnityEvent` wiring and Scene View gizmos for level designers.

**Independent Test**: Wire trigger events to `StartAmbience()`, `StopAmbience()`, and `TriggerRandomAmbient()`; inspect Scene View to confirm volume gizmos delineate sampling boundaries.

- [X] T009 [US3] Implement parameterless public methods (`StartAmbience()`, `StopAmbience()`, `PauseAmbience()`, `ResumeAmbience()`, `TriggerRandomAmbient()`) and `UnityEvent` invocations (`onAmbientStarted`, `onAmbientCompleted`) in `Assets/_Project/Scripts/Core/SpatialAmbientVolume.cs`
- [X] T010 [US3] Implement editor Gizmos (`OnDrawGizmosSelected`) for boundary visualization and emission marker in `Assets/_Project/Scripts/Core/SpatialAmbientVolume.cs`

**Checkpoint**: Level designers can control ambient zones entirely through the Inspector and Scene View.

---

## Phase 6: User Story 4 - Pause & Unscaled Time Safety (Priority: P2)

**Goal**: Ensure interval delays, volume fades, and playback pause cleanly when `Time.timeScale == 0`.

**Independent Test**: Set `Time.timeScale = 0` during active ambience; verify no runaway coroutines or freezes occur and playback resumes cleanly on unpause.

- [X] T011 [US4] Implement pause-safe unscaled timing (`Time.unscaledDeltaTime` / realtime waits) and pause/resume lifecycle handling in `Assets/_Project/Scripts/Core/SpatialAmbientVolume.cs`

**Checkpoint**: Ambient system is pause-safe and respects unscaled time rules.

---

## Phase 7: Polish & Documentation

**Purpose**: Prefab creation, project governance documentation, and validation

- [X] T012 Create `SpatialAmbientVolume.prefab` with default settings and trigger collider in `Assets/_Project/Prefabs/SpatialAmbientVolume.prefab`
- [X] T013 Document feature implementation in `Assets/_Project/Documents/DEVELOPMENT_DIARY.md` per Constitution governance requirements
- [X] T014 Perform code compilation and verification check
