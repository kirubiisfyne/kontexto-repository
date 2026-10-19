# Feature Specification: Spatial Ambient Audio Volume

**Feature Branch**: `[005-spatial-ambient-audio]`

**Created**: 2026-10-16

**Status**: Draft

**Input**: User description: "plays an ambient music in a random place within it parent volume. i want this to be also fit the audio manager that i made."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Randomized In-Volume Spatial Audio Emission (Priority: P1)

As a player exploring a scene or room, I want ambient audio cues and musical atmospheric stingers to periodically originate from random locations within the zone's parent volume, so that the game world feels dynamic, spatialized, and naturally alive rather than emitting from a static 2D loop.

**Why this priority**: Core value of the feature. Without spatially bounded random emission, ambient sounds remain flat and repetitive.

**Independent Test**: Place an ambient zone with a parent `BoxCollider` or `SphereCollider` in a scene, assign multiple audio clips, and enter Play Mode. Verify the sound triggers at varying 3D positions strictly within the parent boundary, with full 3D spatial pan and attenuation as the player navigates.

**Acceptance Scenarios**:

1. **Given** an active ambient volume with a parent `BoxCollider` or `SphereCollider`, **When** the scheduled playback timer elapses, **Then** an ambient audio clip is played at a randomly sampled 3D point strictly inside the volume boundary.
2. **Given** an active ambient audio playback, **When** the player moves closer to or farther from the random emission point, **Then** the volume attenuates according to standard 3D distance rolloff curves (`spatialBlend = 1.0`).
3. **Given** a playlist of multiple audio clips, **When** subsequent sounds trigger, **Then** the system avoids playing the exact same clip consecutively.

---

### User Story 2 - Seamless AudioManager & AudioMixer Integration (Priority: P1)

As a player adjusting settings in the pause menu, I want spatial ambient audio to route through the central `AudioManager` and its AudioMixer channels (`BGMVolume` or `SFXVolume`), so that ambient audio obeys global volume sliders, hardware mute gates, and scene transition fades.

**Why this priority**: Essential architectural requirement. Bypassing `AudioManager` causes unattenuated or unmutable sound, breaking audio consistency and violating project standards.

**Independent Test**: In Play Mode with active ambient volume playback, adjust the Master and target channel volume sliders in the VolumeController/AudioManager. Verify that ambient audio updates synchronously, respects mute thresholds (≤ -80dB / 0.001f), and fades gracefully during scene transitions if configured on BGM.

**Acceptance Scenarios**:

1. **Given** an active ambient volume, **When** audio playback initiates, **Then** the sound routes through `AudioManager.Instance`'s resolved AudioMixer group (configurable by designer to BGM or SFX).
2. **Given** player changes to Master, BGM, or SFX volume in the settings UI or PlayerPrefs, **When** ambient clips play, **Then** their loudness adheres strictly to effective mixer levels and mute thresholds.
3. **Given** an isolated test scene where `AudioManager` is absent, **When** the ambient volume triggers, **Then** it falls back gracefully to local AudioSource playback without throwing null reference exceptions.

---

### User Story 3 - Designer-Centric Playback Control & Volume Triggers (Priority: P2)

As a level designer, I want to attach the ambient audio component to any parent volume, configure timing intervals, volume/pitch randomization, and trigger or stop playback via parameterless `UnityEvent` methods, so that I can configure ambient soundscapes without writing code.

**Why this priority**: Adheres to Constitution Principle II (Designer-Centric Workflow) and Principle I (Isolated Architecture). Allows rooms, gates, cutscenes, and triggers to control ambient playback purely in the Inspector.

**Independent Test**: Hook up a trigger zone or inspector event to `StartAmbience()`, `StopAmbience()`, or `TriggerRandomAmbient()`. Verify that the emitter activates, pauses, or triggers immediately upon invoking the respective event.

**Acceptance Scenarios**:

1. **Given** a designer configuring the component in the Inspector, **When** viewing the component, **Then** all timing (min/max interval), volume jitter, pitch range, 3D distance settings, and audio clips are cleanly exposed with `[Header]`, `[Tooltip]`, and `[SerializeField]`.
2. **Given** parameterless public methods `StartAmbience()`, `StopAmbience()`, `PauseAmbience()`, `ResumeAmbience()`, and `TriggerRandomAmbient()`, **When** wired to scene events (e.g. `RoomController.onRoomEnter`), **Then** ambient audio starts and stops cleanly.
3. **Given** the Unity Editor Scene View, **When** the ambient volume is selected, **Then** editor Gizmos visually outline the parent volume boundary and indicate the last sampled spawn position.

---

### User Story 4 - Pause & Unscaled Time Safety (Priority: P2)

As a player opening the pause menu or engaging in dialogue where the game is paused (`Time.timeScale = 0`), I want ambient playback timing and fade routines to respect the pause state without freezing, breaking coroutines, or desynchronizing audio.

**Why this priority**: Required by Kontexto Audio & UI Standards and Constitution Architectural Constraints.

**Independent Test**: Pause the game (`Time.timeScale = 0`) while an ambient sound is playing or waiting on an interval. Verify audio behavior conforms to game pause rules and unscaled time routines safely resume or halt as configured.

**Acceptance Scenarios**:

1. **Given** an active interval waiting between ambient plays, **When** `Time.timeScale` becomes 0, **Then** the internal timer or coroutine handles pause state predictably without throwing exceptions.
2. **Given** a fade-out or stop triggered while paused, **When** the routine executes, **Then** it uses `Time.unscaledDeltaTime` or `WaitForSecondsRealtime` to complete smoothly.

---

### Edge Cases

- **Parent GameObject has no Collider component**: Component falls back to an inspector-configurable bounding box (`Vector3 volumeSize`) or local bounds, logging an informative warning without crashing.
- **Single audio clip assigned**: Randomizer plays that clip reliably without infinite loops or errors in repeat prevention.
- **Empty audio clip list**: If no clips are assigned, the component logs a gentle warning and idles without spamming errors.
- **Rapid stop/start calls via UnityEvents**: Active coroutines and audio sources cancel cleanly without orphaned sounds, leaked GameObjects, or duplicate interval loops.
- **Overlapping playback**: Option for detached or multi-source playback so that longer ambient tails or musical decays are not abruptly cut off when the next interval triggers.
- **Game object disabled or destroyed**: Component cleanly halts coroutines and destroys temporary spawned emitters.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Component MUST sample random 3D points inside its parent volume (supporting `BoxCollider`, `SphereCollider`, or fallback local bounds), correctly transforming by parent position, rotation, and lossy scale.
- **FR-002**: Component MUST support randomized intervals between plays (configurable `minInterval` and `maxInterval`).
- **FR-003**: Component MUST route audio playback through `AudioManager`'s AudioMixer channel (selectable by designer: `BGM` or `SFX`).
- **FR-004**: Component MUST gracefully fall back to local AudioSource playback if `AudioManager.Instance` is absent from the scene.
- **FR-005**: Component MUST provide designer-friendly pitch variation (`minPitch`, `maxPitch`) and volume scaling/jitter.
- **FR-006**: Component MUST prevent consecutive repeats when multiple audio clips are provided.
- **FR-007**: Component MUST expose parameterless public methods (`StartAmbience()`, `StopAmbience()`, `TriggerRandomAmbient()`, `PauseAmbience()`, `ResumeAmbience()`) for Inspector `UnityEvent` wiring.
- **FR-008**: Component MUST expose `onAmbientStarted` and `onAmbientCompleted` UnityEvents.
- **FR-009**: Component MUST support 3D spatial settings (`spatialBlend = 1.0`, configurable min/max distance, attenuation rolloff).
- **FR-010**: Component MUST support editor gizmos (`OnDrawGizmosSelected` / `OnDrawGizmos`) showing parent volume bounds and recent spawn position.
- **FR-011**: Component MUST support unscaled time handling for fade and interval operations to maintain stability during game pause (`Time.timeScale = 0`).

### Key Entities

- **SpatialAmbientVolume**: Core MonoBehaviour attached to a volume or parent trigger, managing random point sampling, clip selection, interval coroutines, and spatial AudioSource positioning.
- **AudioChannel**: Enum selector (`BGM`, `SFX`) determining which AudioMixer group on `AudioManager` receives the output.
- **AudioManager**: Core persistent singleton providing resolved AudioMixer channels, master/channel volume levels, and global sound configuration.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of generated ambient playback coordinates reside within the specified volume collider or bounds.
- **SC-002**: Audio volume directly reflects master and channel volume changes made in `AudioManager` with zero perceptible desync or double attenuation.
- **SC-003**: Zero `NullReferenceException` occurrences when run in an isolated test scene without `AudioManager`.
- **SC-004**: All cross-system trigger interactions (e.g. entering a room or completing a quest step) can trigger or stop ambient zones via Unity Inspector without writing additional glue code.
- **SC-005**: Game pause (`Time.timeScale = 0`) does not halt fade coroutines or cause runaway audio loops.

## Assumptions

- The parent volume is primarily defined by a Unity `BoxCollider` or `SphereCollider` (which may be set as `isTrigger` or disabled for physics).
- Ambient sounds are either atmospheric music tracks/motifs (routed through BGM mixer group) or environmental sound cues (routed through SFX mixer group).
- Standard 3D audio listener is attached to the player or main camera.
