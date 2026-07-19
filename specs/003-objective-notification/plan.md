# Implementation Plan: Objective Progress Notification

**Branch**: `[003-objective-notification]` | **Date**: 2026-07-19 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/003-objective-notification/spec.md`

## Summary

Implement a transient, decoupled notification UI for objective progress that fades in to display updates (e.g., "1/4") and fades out after a delay. It relies entirely on `UnityEvent` triggers to adhere to the project's isolated architecture.

## Technical Context

**Language/Version**: C# (Unity)
**Primary Dependencies**: Unity UI (UGUI / TextMeshPro), `UnityEngine.Events`
**Storage**: N/A
**Testing**: Unity Play Mode testing (Designer visual validation)
**Target Platform**: Unity Supported Platforms
**Project Type**: Game UI System
**Performance Goals**: Minimal GC allocation during text updates; UI animations must use unscaled time.
**Constraints**: Strict adherence to "Dumb" Systems (no cross-system hard references), Unscaled UI for pause compatibility.
**Scale/Scope**: Single UI prefab instantiated per scene or globally.

## Constitution Check

*GATE: Passed*

- **I. Pure & Isolated Architecture**: The UI will expose parameterless or simple parameter methods (e.g., `ShowProgress(string text)`) intended to be wired via `UnityEvent`. It will not reference the Task system directly.
- **II. Designer-Centric Workflow**: Exposed fields for fade durations, display durations, and colors will be tagged with `[Header]`, `[Tooltip]`, and `[SerializeField]`.
- **III. Predictability**: Simple state management for showing/hiding to prevent overlap/flickering issues.
- **Architecture Constraints (Unscaled UI)**: UI animations will use `unscaledDeltaTime` to function properly even if `Time.timeScale == 0`.

## Project Structure

### Documentation (this feature)

```text
specs/003-objective-notification/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
└── quickstart.md        # Phase 1 output
```

### Source Code (repository root)

```text
Assets/
├── Scripts/
│   └── UI/
│       └── ObjectiveNotificationUI.cs
└── Prefabs/
    └── UI/
        └── ObjectiveNotificationCanvas.prefab
```

**Structure Decision**: Added script and prefab under the existing standard Unity project structure.

## Complexity Tracking

*(No constitution violations. Kept as simple as possible).*
