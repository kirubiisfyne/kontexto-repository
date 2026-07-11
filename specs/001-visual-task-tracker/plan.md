# Implementation Plan: Visual Task Tracker

**Branch**: `001-visual-task-tracker` | **Date**: 2026-07-10 | **Spec**: [specs/001-visual-task-tracker/spec.md](file:///Users/kirubiisfyne/desktop/creatives/unity/unity%20projects/kontexto-repository/specs/001-visual-task-tracker/spec.md)

**Input**: Feature specification from `/specs/001-visual-task-tracker/spec.md`

## Summary

Add a visual task tracker UI based on the current level. The UI will leverage the existing `LevelTaskTracker` to populate active tasks and update in real-time when tasks are completed. It must adhere to the Inspector-first workflow and run on Unscaled Time to support pausing.

## Technical Context

**Language/Version**: C# (Unity 2022+)

**Primary Dependencies**: Unity UI (UGUI / TextMeshPro), UnityEvents

**Storage**: N/A (UI only reads from existing `LevelTaskTracker` which delegates to `SaveGameBridge`)

**Testing**: In-editor manual verification using Inspector-driven events

**Target Platform**: Unity Standalone / Mobile

**Project Type**: Unity Game Feature / UI System

**Performance Goals**: Instant UI updates, no measurable frame drop during animations, supports unscaled time.

**Constraints**: Must be completely decoupled from existing task scripts (only communicate via UnityEvents or observing public state), no singletons.

**Scale/Scope**: ~5 simultaneous tasks per level. Small codebase footprint (1-2 new UI scripts).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Pure & Isolated Architecture**: Passed. The UI will not hard-reference the task system's internal logic, and relies on `UnityEvent` or observer patterns hooked up in the Inspector.
- **II. Designer-Centric Workflow**: Passed. Exposing public parameterless methods and UnityEvents on `TaskTrackerUI` and extending `LevelTaskTracker` with `UnityEvent` for updates.
- **III. Predictability & Robust State Management**: Passed. UI state is a direct reflection of `LevelTaskTracker`'s authoritative list of active tasks.
- **IV. Reliable Data Persistence & Scene-Local Execution**: Passed. Using scene-local `LevelTaskTracker` rather than a global singleton.
- **V. Modular & Self-Contained Components**: Passed. The `TaskTrackerUI` only cares about displaying strings/states and doesn't manage task progression itself.

## Project Structure

### Documentation (this feature)

```text
specs/001-visual-task-tracker/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output
```

### Source Code (repository root)

```text
Assets/
└── Master/
    ├── Prefabs/
    │   └── UI/
    │       ├── TaskTrackerUI.prefab
    │       └── TaskItemUI.prefab
    └── Scripts/
        ├── SaveSystem/
        │   └── LevelTaskTracker.cs (modified to expose events)
        └── UI/
            ├── TaskTrackerUI.cs
            └── TaskItemUI.cs
```

**Structure Decision**: The UI scripts will live under `Assets/Master/Scripts/UI`, separated from the core save/task system. The prefabs will be in `Assets/Master/Prefabs/UI`. We will modify the existing `LevelTaskTracker` in place.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No violations detected.
