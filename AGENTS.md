# Agent Instructions

## Response Format & Communication
- **TL;DR Summaries**: Always include a concise `## TL;DR` at the start of responses (or immediately following status headers) summarizing the key takeaway, decisions made, or core action items before diving into technical explanations, code breakdowns, or deep architectural details.

## Project Guidelines & Architecture (Kontexto)
- **Engine**: Unity (C#) using `Project.asmdef`.
- **Pure & Isolated Architecture ("Dumb" Systems)**: Systems MUST NOT directly depend on or reference each other across distinct domains (e.g., TaskSystem, DialogueSystem, RoomSystem). Cross-system logic must be decoupled and wired in the Unity Inspector via `UnityEvent`.
- **Designer-Centric Workflow (Inspector First)**: Expose clean Inspector interfaces with `[SerializeField]`, `[Header]`, and `[Tooltip]`. Provide explicit, parameterless public methods (e.g., `SaveGame()`, `CompleteTask()`) designed for `UnityEvent` wiring.
- **Predictability & State Management**: Prioritize explicit state transitions and immediate snapshots for critical interactions (e.g. Dialogue snapshot on input).
- **Audio & UI Standards**:
  - Route all game audio through `AudioManager` and the main AudioMixer channels (`MasterVolume`, `BGMVolume`, `SFXVolume`).
  - UI animators and transitions must support unscaled time (`Time.unscaledTime` / `unscaledDeltaTime`) so menus and dialogues function safely during pause (`Time.timeScale = 0`).
