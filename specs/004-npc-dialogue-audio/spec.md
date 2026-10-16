# Feature Specification: NPC Dialogue Audio Feedback

**Feature Branch**: `[004-npc-dialogue-audio]`

**Created**: 2026-10-16

**Status**: Draft

**Input**: User description: "When triggering a dialogue to the NPC it plays an audio, and when skipping or next lining."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Dialogue Initiation Audio Cue (Priority: P1)

As a player, when I approach an NPC and trigger a conversation, I want an immediate audio cue (an interaction chime or character-specific voice greeting) to play, so that I receive satisfying sensory feedback confirming the dialogue interaction has begun.

**Why this priority**: Establishes the entry point of every NPC interaction. Without audio feedback on interaction, initiating dialogue feels unresponsive.

**Independent Test**: Walk up to an NPC and press the interact key (`F`). Verify the dialogue window opens and the dialogue start/greeting audio cue plays immediately.

**Acceptance Scenarios**:

1. **Given** the player is in interaction range of an NPC whose dialogue is ready, **When** the player presses the interact key, **Then** the dialogue UI appears and the dialogue start audio cue plays.
2. **Given** an NPC whose dialogue is on cooldown or inactive, **When** the player presses the interact key, **Then** no dialogue audio cue plays and the dialogue window remains closed.
3. **Given** an NPC with a character-specific greeting audio clip assigned, **When** the dialogue begins, **Then** the character-specific greeting plays instead of (or in conjunction with) the default dialogue open sound.

---

### User Story 2 - Typewriter Skip Audio Feedback (Priority: P1)

As a player, when an NPC's dialogue text is actively revealing character-by-character and I press the interact key to skip, I want the full sentence to appear immediately accompanied by a snappy skip audio cue, so that I know the text reveal was fast-forwarded.

**Why this priority**: Fast readers and players replaying conversations frequently skip text. Providing audio feedback for skipping reinforces responsive game feel and stops lingering typing blips cleanly.

**Independent Test**: Initiate dialogue with an NPC. While the text is animating, press `F`. Verify the sentence reveals completely, typewriter blip audio stops, and a distinct skip sound cue plays.

**Acceptance Scenarios**:

1. **Given** dialogue text is actively animating across the screen, **When** the player presses the skip/interact key, **Then** the remaining characters appear instantly, active typewriter character blips cease, and a skip audio cue plays.
2. **Given** text that has already fully revealed, **When** the player presses the interact key, **Then** the skip audio cue does NOT play (the action is treated as advancing to the next line instead).

---

### User Story 3 - Next Line Dialogue Progression Audio (Priority: P1)

As a player, when a dialogue line has finished displaying and I press the interact key to proceed to the next line, I want to hear a clean dialogue progression sound (such as a page-turn or progression click), so that advancing through conversation branches feels tactile and clear.

**Why this priority**: Core feedback loop for consuming multi-line conversations, distinguishing advancing between lines from initiating or skipping text.

**Independent Test**: In a multi-line conversation where the current line has finished revealing, press `F`. Verify the next line audio cue plays and the subsequent line starts typing.

**Acceptance Scenarios**:

1. **Given** the current dialogue line has finished revealing and subsequent lines remain in the conversation queue, **When** the player presses the interact key, **Then** the next-line progression audio cue plays and the next dialogue line begins typing.
2. **Given** the final line of a conversation has finished revealing, **When** the player presses the interact key, **Then** the dialogue ends, the dialogue UI closes, and either a dialogue close audio cue plays or no next-line cue is played.

---

### Edge Cases

- **Rapid Input Spamming**: When the player rapidly mashes the interact key (`F`), audio cues must not cut out unnaturally or crash the audio engine. AudioSources should handle overlapping one-shots cleanly with pitch modulation or throttling.
- **Missing Audio Clips / Unassigned Sounds**: If an NPC or dialogue UI does not have explicit audio clips assigned, the system must gracefully fall back to default sound identifiers (or silent fallback) without null reference exceptions.
- **Volume Settings & Audio Mixer Synchronization**: All dialogue SFX must route through the Audio Mixer SFX channel and obey PlayerPrefs volume and mute states configured in `AudioManager`.
- **Game Paused During Dialogue**: If the player pauses the game during dialogue, unscaled UI audio playback must respect pause audio behavior without continuing to loop indefinitely.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST play an audio cue when an NPC dialogue interaction is successfully triggered.
- **FR-002**: System MUST play a distinct audio cue when the player skips an active typewriter text reveal.
- **FR-003**: System MUST play an audio cue when the player advances from one dialogue line to the next in an active conversation.
- **FR-004**: System MUST differentiate between skipping an in-progress line, advancing to a subsequent line, and dismissing the final line of dialogue.
- **FR-005**: All dialogue audio cues MUST route through `AudioManager`'s SFX channel (or an assigned AudioSource with the SFX mixer group) to respect player volume preferences.
- **FR-006**: Audio configuration MUST be designer-accessible in the Unity Inspector (via `AudioClip` serialized fields or `AudioManager` sound registry names) per Constitution Principle II.
- **FR-007**: System MUST support speaker-specific audio customization (e.g. via `SpeakerProfile` in `SpeakerDatabase` or `DialogueManager` per-NPC fields) with fallback to default UI sounds.

### Key Entities

- **DialogueManager**: Initiates dialogue interactions on NPCs, tracks conversation queues, and coordinates line progression and completion.
- **DialogueUIManager**: Controls presentation of dialogue text, portrait rendering, typewriter animation, and UI-level audio cues (open, skip, next line, close).
- **SpeakerDatabase / SpeakerProfile**: ScriptableObject mapping speaker identifiers to character portraits and speaker-specific voice or greeting audio clips.
- **AudioManager**: Core singleton managing sound effects, audio mixer routing, volume scaling, and pitch-randomized playback.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of dialogue state transitions (dialogue start, typewriter skip, next line, dialogue close) produce their designated audio feedback with zero noticeable latency (<50ms).
- **SC-002**: Zero null reference exceptions or audio errors when interacting with NPCs that lack optional custom audio clips.
- **SC-003**: Rapidly pressing the advance key (up to 10 presses per second) results in clean, non-clipping audio playback and consistent text progression.
- **SC-004**: Sound cues strictly adhere to user volume controls and mute settings in the audio options menu.

## Assumptions

- The project uses the existing `AudioManager` and `DialogueUIManager` components for global SFX and dialogue management.
- Dialogue text progression continues to be triggered primarily via the `F` key (or mapped interact key via `UniversalInteractor` / `DialogueManager`).
- Audio assets (e.g. dialogue open, line skip, line advance clips) will be mapped either directly as `AudioClip` references on `DialogueUIManager` or registered in `AudioManager.sfxSounds`.
