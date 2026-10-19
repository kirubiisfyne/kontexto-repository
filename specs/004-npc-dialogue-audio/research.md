# Phase 0: Research & Technical Decisions

## 1. Dual-Resolution Voice Cue Hierarchy

- **Decision**: Implement a 4-tier fallback hierarchy for resolving the vocal audio clip ("hmm, ahh!"):
  1. **Direct NPC Override**: `DialogueManager.LastInteracted.VoiceClip` (allows level designers to test or give a unique voice directly on an NPC GameObject without editing data assets).
  2. **Character Profile**: `SpeakerDatabase.GetProfile(speakerName).GetRandomVoiceClip()` (ScriptableObject-driven character identity, allowing multiple randomized vocal clips and pitch tuning).
  3. **Global Default Vocal SFX**: `DialogueUIManager.defaultVoiceSFX` (applies to all dialogue when speaker-specific clips are omitted).
  4. **Fallback Audio Name**: `AudioManager.PlaySFX(fallbackName)` (graceful fallback using globally registered audio or silent degradation if unassigned).
- **Rationale**:
  - Complies with Constitution Principle II (Designer-Centric Workflow): designers can test locally on an NPC, organize globally in `SpeakerDatabase`, or rely on a baseline UI voice sound.
  - Zero null reference exceptions even if no audio assets are assigned.
- **Alternatives considered**:
  - *Hardcoding clip names in JSON*: Rejected because dialogue JSON files are narrative scripts; embedding audio asset paths couples content writing to asset serialization.
  - *Separate audio controllers per NPC*: Rejected as over-engineering; `DialogueManager` already handles interaction flow and `DialogueUIManager` already renders the text.

## 2. Audio Routing & Mixer Integration

- **Decision**: 
  - Extend `AudioManager` with a direct `PlaySFX(AudioClip clip, bool randomPitch = false, float minPitch = 0.85f, float maxPitch = 1.15f, float volumeScale = 1f)` overload.
  - In `DialogueUIManager`, route audio through `AudioManager.Instance.PlaySFX()` when present, or fall back to its internal `typingAudioSource` with the SFX mixer group.
- **Rationale**:
  - Adheres to Kontexto Audio Standards: "Route all game audio through AudioManager and the main AudioMixer channels (`MasterVolume`, `BGMVolume`, `SFXVolume`)."
  - Avoids spawning unnecessary temporary GameObjects while respecting user SFX volume settings and mute gates.
- **Alternatives considered**:
  - *Spawning temporary detached AudioSources (`AudioSource.PlayClipAtPoint`)*: Rejected because `PlayClipAtPoint` does not route through the Unity AudioMixer and cannot be controlled by in-game volume settings.

## 3. Input Discrimination: Start, Skip, and Next Line

- **Decision**:
  - **Dialogue Start**: In `DialogueManager.StartDialogueRoutine()`, before or upon displaying the first line, the vocal cue is played. Since `DisplayNextLine()` is called for line 0, triggering it when the view updates or during session start provides immediate tactile audio.
  - **Typewriter Skip**: In `DialogueManager.DisplayNextLine()`, when `DialogueUIManager.Instance.IsTyping` is true, `FinishTyping()` is invoked. Playing the vocal/skip SFX here gives immediate auditory confirmation of the skip.
  - **Next Line Advance**: When `IsTyping` is false and `linesQueue.Count > 0`, `DisplayNextLine()` dequeues the next line and calls `DialogueUIManager.Instance.UpdateDialogueView(currentLine)`, triggering the next line vocal cue.
  - **Dialogue End**: When `linesQueue.Count == 0`, `EndDialogue()` is called. No next line cue is played; an optional dialogue close cue or silence occurs.
- **Rationale**:
  - Solves requirement FR-004 cleanly using the exact state booleans already maintained by `DialogueManager` and `DialogueUIManager`.
- **Alternatives considered**:
  - *Listening directly to `Input.GetKeyDown(KeyCode.F)` inside `DialogueUIManager`*: Rejected because `DialogueManager` is the authoritative controller of dialogue progression; having multiple components poll `F` introduces race conditions.
