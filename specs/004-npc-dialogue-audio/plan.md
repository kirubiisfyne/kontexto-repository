# Implementation Plan: NPC Dialogue Audio Feedback

**Branch**: `[004-npc-dialogue-audio]` | **Date**: 2026-10-19 | **Spec**: [specs/004-npc-dialogue-audio/spec.md](./spec.md)

**Input**: Feature specification from `specs/004-npc-dialogue-audio/spec.md` ("triggering a dialogue with an npc plays a 'hmm, ahh!' sfx, which also will play when skipping or next linning.")

## Summary

Implement a responsive, designer-configurable dialogue audio feedback system that plays vocal cues / sound effects (such as "hmm, ahh!" NPC voice expressions) when initiating dialogue with an NPC, skipping the typewriter text reveal, and advancing to subsequent dialogue lines. The audio architecture supports global defaults on `DialogueUIManager`, optional per-character voice profiles on `SpeakerDatabase` (`SpeakerProfile`), and optional direct NPC overrides on `DialogueManager`, routing all playback through `AudioManager`'s SFX channel and AudioMixer group with pitch modulation and volume control.

## Technical Context

**Language/Version**: C# (.NET Standard 2.1 / Unity 2022+ C#)  
**Primary Dependencies**: Unity Audio (`AudioSource`, `AudioMixer`, `AudioMixerGroup`), `Project.asmdef` (`Master.Scripts`, `Master.Scripts.DialogueSystem`)  
**Storage**: N/A (State is runtime; speaker profiles live in `MainSpeakerDB.asset`, audio levels persist via `AudioManager` PlayerPrefs)  
**Testing**: Unity Play Mode validation (NPC dialogue initiation, skipping, and line progression audio cues)  
**Target Platform**: PC / Standalone (Unity supported platforms)  
**Project Type**: Unity Gameplay / Dialogue & Audio System  
**Performance Goals**: Allocation-free playback; pitch-jittered one-shot calls through `AudioManager.sfxSource`  
**Constraints**:
- Strict adherence to Constitution Principle I: Pure & Isolated Architecture ("Dumb" Systems).
- Strict adherence to Constitution Principle II: Designer-Centric Workflow (Inspector First).
- Strict adherence to Audio & UI Standards: Route audio through `AudioManager` / AudioMixer, pause safety using unscaled time.
**Scale/Scope**: Dialogue playback across all NPC conversations throughout campus and classroom scenes.

## Constitution Check

*GATE: Passed*

- **I. Pure & Isolated Architecture ("Dumb" Systems)**: `DialogueManager` and `DialogueUIManager` do not couple to task or save systems. Audio playback interacts with `AudioManager` through clean, null-safe helper methods. If `AudioManager.Instance` is absent, audio degrades gracefully to local `AudioSource` or silent fallback without throwing exceptions.
- **II. Designer-Centric Workflow (Inspector First)**: Serialized fields for default vocal SFX, skip SFX, next line SFX, pitch variation ranges, and volume are exposed with `[Header]`, `[Tooltip]`, and `[Range]` attributes. Character-specific voice clips are cleanly configurable in `SpeakerDatabase` ScriptableObject assets.
- **III. Predictability & Robust State Management**: Clean discrimination between typing state (`IsTyping`), end of conversation (`linesQueue.Count == 0`), and advancing lines prevents audio misfires or duplicate sound overlap.
- **IV. Reliable Data Persistence & Scene-Local Execution**: No persistent data mutation required; respects player audio settings.
- **V. Modular & Self-Contained Components**: Leverages existing `DialogueManager`, `DialogueUIManager`, `SpeakerDatabase`, and `AudioManager` without creating unnecessary intermediate manager components.
- **Audio & UI Standards**: Playback uses unscaled time safety and routes directly through AudioMixer's `SFXVolume` group.

## Project Structure

### Documentation (this feature)

```text
specs/004-npc-dialogue-audio/
├── spec.md              # Feature specification
├── checklists/
│   └── requirements.md  # Quality validation checklist
├── plan.md              # This file
├── research.md          # Phase 0: Technical decisions & research
├── data-model.md        # Phase 1: Data entities, Inspector fields, states
├── quickstart.md        # Phase 1: Step-by-step validation guide
└── tasks.md             # Implementation tasks
```

### Source Code (repository root)

```text
Assets/_Project/Scripts/
├── Core/
│   └── AudioManager.cs                # Extended with PlaySFX(AudioClip, ...) overload
└── Gameplay/
    └── DialogueSystem/
        ├── SpeakerDatabase.cs         # Extended SpeakerProfile with voiceClips and voicePitch
        ├── DialogueUIManager.cs       # Audio configuration, fallback resolution & playback
        └── DialogueManager.cs         # Triggers voice SFX on start, skip, and line advance
```

## Complexity Tracking

*(No constitution violations. Reuses existing architecture and Inspector patterns).*
