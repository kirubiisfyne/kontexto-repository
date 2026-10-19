# Implementation Plan: Spatial Ambient Audio Volume

**Branch**: `[005-spatial-ambient-audio]` | **Date**: 2026-10-16 | **Spec**: [specs/005-spatial-ambient-audio/spec.md](./spec.md)

**Input**: Feature specification from `specs/005-spatial-ambient-audio/spec.md`

## Summary

Implement a decoupled, designer-centric 3D ambient audio system (`SpatialAmbientVolume`) that periodically plays spatialized atmospheric music, soundscapes, or environmental cues at random 3D coordinates within its parent volume (such as a `BoxCollider` or `SphereCollider`). The component routes its audio through `AudioManager`'s AudioMixer channels (`BGMVolume` or `SFXVolume`) while gracefully degrading in isolated test scenes, fully respecting Kontexto's Isolated Architecture ("Dumb" Systems), Designer-Centric Workflow, and pause-safe unscaled timing standards.

## Technical Context

**Language/Version**: C# (.NET Standard 2.1 / Unity 2022+ C#)  
**Primary Dependencies**: Unity Audio (`AudioSource`, `AudioMixer`, `AudioMixerGroup`), `UnityEngine.Events`, `Project.asmdef` (`Master.Scripts`)  
**Storage**: N/A (State is scene-local; volume preferences persist through existing `AudioManager` PlayerPrefs)  
**Testing**: Unity Play Mode validation (visual gizmo confirmation and audio attenuation testing)  
**Target Platform**: PC / Standalone (Unity supported platforms)  
**Project Type**: Unity Game Audio / Environment System  
**Performance Goals**: 0 runtime GC allocations during ambient playback intervals; reused child emitter transform  
**Constraints**: 
- Strict adherence to Constitution Principle I: Pure & Isolated Architecture ("Dumb" Systems).
- Strict adherence to Constitution Principle II: Designer-Centric Workflow (Inspector First, parameterless `UnityEvent` methods).
- Strict adherence to Audio & UI Standards: Route audio through `AudioManager` / AudioMixer, pause safety using unscaled time.
**Scale/Scope**: Scene-level and room-level ambient audio volume component placed on environment zone prefabs.

## Constitution Check

*GATE: Passed*

- **I. Pure & Isolated Architecture ("Dumb" Systems)**: `SpatialAmbientVolume` does not hold hard references to external room or task managers. It exposes parameterless public methods (`StartAmbience()`, `StopAmbience()`, `TriggerRandomAmbient()`) designed for `UnityEvent` wiring. If `AudioManager.Instance` is absent, it falls back to local audio without throwing exceptions.
- **II. Designer-Centric Workflow (Inspector First)**: All settings (clip variations, min/max intervals, volume/pitch jitter, 3D distance rolloff, channel selection) are cleanly categorized with `[Header]`, `[Tooltip]`, `[SerializeField]`, and `[Range]` attributes. Editor gizmos render volume boundaries and sampled points in Scene View.
- **III. Predictability & Robust State Management**: Uses an explicit state enum (`AmbientState`: `Inactive`, `WaitingInterval`, `Playing`, `Paused`) to prevent race conditions or duplicate overlapping interval loops.
- **IV. Reliable Data Persistence & Scene-Local Execution**: The volume operates purely scene-locally. Volume settings are driven by the centralized `AudioManager`.
- **V. Modular & Self-Contained Components**: Volume detection, point sampling, interval timing, and 3D emission are unified in a single, coherent component without gratuitous splitting.
- **Audio & UI Standards**: Audio routes to `AudioManager` AudioMixer channels (`BGM` or `SFX`). Timing logic handles pause states safely.

## Project Structure

### Documentation (this feature)

```text
specs/005-spatial-ambient-audio/
├── spec.md              # Feature specification
├── checklists/
│   └── requirements.md  # Quality validation checklist
├── plan.md              # This file
├── research.md          # Phase 0: Technical decisions & research
├── data-model.md        # Phase 1: Data entities, Inspector fields, states
└── quickstart.md        # Phase 1: Step-by-step validation guide
```

### Source Code (repository root)

```text
Assets/_Project/
├── Scripts/
│   └── Core/
│       ├── AudioManager.cs         # Extended with GetChannelGroup(AudioChannel)
│       └── SpatialAmbientVolume.cs # New: core spatial ambient volume component
└── Prefabs/
    └── Audio/
        └── SpatialAmbientVolume.prefab # Designer prefab with BoxCollider + component
```

**Structure Decision**: Placed `SpatialAmbientVolume.cs` alongside `AudioManager.cs` and `LocalAudioPlayer.cs` in `Assets/_Project/Scripts/Core/` under the `Master.Scripts` namespace within `Project.asmdef`. Created a reusable designer prefab in `Assets/_Project/Prefabs/Audio/`.

## Complexity Tracking

*(No constitution violations. Kept as simple, modular, and self-contained as possible).*
