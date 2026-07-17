# Implementation Plan: Transition and Gate System Refactor

**Branch**: `002-transition-gate-remake` | **Date**: 2026-07-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-transition-gate-remake/spec.md`

## Summary

Refactor the transition and gate system to ensure robust Animator state evaluation that prevents race conditions, replace string-based scene matching with explicit Gate IDs for better spawn reliability, and enable flexible scene loading with async operations that wait for transitions to finish.

## Technical Context

**Language/Version**: C# 9.0 (Unity)

**Primary Dependencies**: Unity Engine (Animator, SceneManagement, Coroutines)

**Storage**: Unity JSON Save System (already existing, untouched)

**Testing**: Unity Play Mode / Manual testing of transitions

**Target Platform**: Desktop (Windows/Mac)

**Project Type**: Unity 3D Game

**Performance Goals**: Consistent 60fps; transition stutters must be eliminated.

**Constraints**: Transition UI must work when `Time.timeScale = 0`.

**Scale/Scope**: Impacts all scene loading and UI-driven transitions across the game.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **I. Pure & Isolated Architecture**: The refactored gate system relies on string IDs instead of tight coupling. It does not introduce new inter-system dependencies.
- [x] **II. Designer-Centric Workflow**: Gate IDs are exposed as `string` fields in the Inspector, allowing designers to set up doors without code.
- [x] **III. Predictability & Robust State Management**: The core of this refactor is to enforce robust state management by ensuring Animator states are correctly initialized before lengths are queried, aligning perfectly with this principle.
- [x] **IV. Reliable Data Persistence**: Does not interfere with the existing SaveSystem; `SceneGateInstance` continues to call `SaveGame()` on warp.
- [x] **V. Modular & Self-Contained**: The `TransitionManager` and `SceneGateManager` remain self-contained singletons handling visual fading and loading mechanics respectively.

## Project Structure

### Documentation (this feature)

```text
specs/002-transition-gate-remake/
├── plan.md              
├── research.md          
├── data-model.md        
├── quickstart.md        
```

### Source Code (repository root)

```text
Assets/Master/Scripts/
├── Core/
│   └── TransitionManager.cs
├── SceneManagement/
│   ├── SceneGateManager.cs
│   └── SceneGateInstance.cs
```

**Structure Decision**: Using existing Unity project structure under `Assets/Master/Scripts/`.
