# Phase 1: Quickstart & Validation Guide

## Setup

1. Open the Unity project.
2. Create or navigate to a test scene with a `Main Camera` containing an `AudioListener`.
3. Ensure the `AudioManager` prefab is present in the scene (or verify fallback in an empty scene).
4. Create an empty GameObject named `Room_AmbienceZone`.
5. Add a `BoxCollider` (e.g. Size = `(15, 6, 15)`, `isTrigger = true`).
6. Add the `SpatialAmbientVolume` component to `Room_AmbienceZone` (or as a child).
7. In the Inspector, assign 2-3 atmospheric audio clips to `Ambient Clips`.
8. Set `Min Interval = 3`, `Max Interval = 6` for rapid testing.

---

## Validation Scenarios

### Scenario 1: Spatial In-Volume Point Randomization
- **Action**: Enter Play Mode with Scene View and Game View visible side by side.
- **Expected Outcome**:
  - The component enters `WaitingInterval`, waits 3-6 seconds, and emits audio.
  - The child emitter moves to a position strictly within the `BoxCollider` boundaries.
  - In Scene View gizmos, the sampled position is visibly contained within the cyan box outline.
  - If multiple clips exist, consecutive sounds cycle without immediate repeat.

### Scenario 2: 3D Spatial Attenuation
- **Action**: In Play Mode, move the Player or Main Camera closer to and further away from the emission point while a clip is playing.
- **Expected Outcome**:
  - Audio pans left/right according to the emitter's direction relative to the AudioListener.
  - Volume attenuates smoothly as distance increases towards `maxDistance`.

### Scenario 3: AudioManager & AudioMixer Routing
- **Action**: Set `Channel = AudioChannel.BGM` on the component. In Play Mode, open the Pause/Options menu or inspect the `VolumeController` sliders.
- **Expected Outcome**:
  - Adjusting the `MasterVolume` slider scales ambient loudness.
  - Adjusting the `BGMVolume` slider scales ambient loudness.
  - Setting `BGMVolume` or `MasterVolume` to 0 completely silences the ambient playback without errors.

### Scenario 4: Parameterless UnityEvent Control
- **Action**: Uncheck `Play On Start`. Create a test UI Button (or invoke in inspector via a trigger) hooked to `StartAmbience()`, `StopAmbience()`, and `TriggerRandomAmbient()`.
- **Expected Outcome**:
  - `TriggerRandomAmbient()` immediately triggers a sound at a new random point without waiting for the interval timer.
  - `StopAmbience()` halts ongoing audio and stops interval scheduling.
  - `StartAmbience()` resumes the interval loop.

### Scenario 5: Isolated Scene Fallback (Constitution Principle I)
- **Action**: Open a brand-new empty scene without `AudioManager.prefab`. Add `SpatialAmbientVolume` with a `BoxCollider` and audio clip. Enter Play Mode.
- **Expected Outcome**:
  - Audio plays normally using the local `AudioSource` settings.
  - Zero `NullReferenceException` or missing reference errors are logged.

### Scenario 6: Game Pause Stability (Unscaled Time)
- **Action**: While an ambient clip is scheduled or playing, set `Time.timeScale = 0`.
- **Expected Outcome**:
  - If `pauseWithGame = true`, playback cleanly pauses and resumes when `Time.timeScale = 1`.
  - Coroutines do not lock up or crash.
