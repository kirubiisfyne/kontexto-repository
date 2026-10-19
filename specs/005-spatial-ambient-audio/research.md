# Phase 0: Research & Technical Decisions

## 1. Parent Volume Geometry & Point Sampling

- **Decision**: Support Unity `BoxCollider` and `SphereCollider` primitives (either on the same GameObject or resolved via `GetComponentInParent<Collider>()`), with a fallback to local bounding extents (`Vector3 fallbackBoundsSize`).
- **Rationale**: 
  - Standard level design workflow in Unity uses colliders (often trigger zones) to designate rooms, courtyards, corridors, and exterior zones.
  - Using local space sampling transformed via `Transform.TransformPoint()` properly accounts for parent position, rotation, and non-uniform scaling:
    - **BoxCollider**: Local point sampled within `[-size.x/2, size.x/2]`, `[-size.y/2, size.y/2]`, `[-size.z/2, size.z/2]` offset by `box.center`, then transformed to world coordinates.
    - **SphereCollider**: Local point sampled via `Random.insideUnitSphere * radius + sphere.center`, then transformed to world coordinates.
- **Alternatives considered**:
  - *NavMesh point sampling (`NavMesh.SamplePosition`)*: Rejected because ambient sounds (wind, birds, musical chimes, distant bells) frequently originate above ground, on ceilings, or outside walkable navmesh geometry.
  - *MeshCollider arbitrary volume sampling*: Overly complex; raycasting arbitrary non-convex meshes introduces runtime CPU overhead and GC allocation. Box and Sphere colliders cover 99% of zone definitions.

## 2. AudioManager & AudioMixer Routing Architecture

- **Decision**: 
  - Route the ambient `AudioSource` directly into `AudioManager`'s AudioMixer channel (selectable by designer as `BGM` or `SFX`).
  - Extend `AudioManager` with a lightweight, parameter-safe helper `GetChannelGroup(AudioChannel channel)` that returns `bgmSource.outputAudioMixerGroup` or `sfxSource.outputAudioMixerGroup` (or resolves via `ResolveMixer().FindMatchingGroups(channel)`).
  - Include an immediate graceful fallback in `SpatialAmbientVolume`: if `AudioManager.Instance` is null (e.g. running an isolated test scene), leave mixer group unassigned or fallback to direct AudioSource playback without throwing any exceptions.
- **Rationale**:
  - Kontexto Audio Standards mandate: "Route all game audio through AudioManager and the main AudioMixer channels (`MasterVolume`, `BGMVolume`, `SFXVolume`)."
  - Constitution Principle I mandates "Pure & Isolated Architecture ('Dumb' Systems)": neither system should crash or fail if the other is absent.
  - Constitution Principle II mandates Designer-Centric Workflow: allowing the designer to choose whether a sound is ambient music (BGM) or ambient SFX ensures proper slider control in menus.
- **Alternatives considered**:
  - *Playing all ambient sounds through the 2D `AudioManager.Instance.PlayBGM`*: Rejected because 2D playback cannot spatialize sound at random 3D points inside the room volume.
  - *Creating a fourth audio channel ("Ambient") in the AudioMixer*: Unnecessary complexity; existing `am_Main.mixer` has `BGM` and `SFX` groups and the UI `VolumeController` has Master, BGM, and SFX sliders. Mapping ambient music to `BGM` and ambient cues to `SFX` integrates seamlessly with existing UI sliders without requiring mixer asset migration.

## 3. AudioSource Positioning & Lifecycle Strategy

- **Decision**: 
  - Use a dedicated child GameObject emitter (`_activeEmitter`) containing the 3D `AudioSource`.
  - When a sound triggers:
    1. Sample random 3D world coordinate within volume.
    2. Position `_activeEmitter.transform.position = sampledPoint`.
    3. Configure 3D spatial settings (`spatialBlend = 1f`, distance rolloff, pitch, volume).
    4. Play audio clip.
  - Provide an optional `allowOverlap` mode using a small 2-3 emitter pool to prevent cutting off long ambient reverberation tails if a subsequent sound triggers before the previous finishes.
- **Rationale**:
  - Zero runtime allocations per play in standard mode.
  - Preserves clean transform hierarchy: child emitter moves within local/world space cleanly.
- **Alternatives considered**:
  - *Instantiating temporary GameObjects on every play (detached like `LocalAudioPlayer.playDetached`)*: Creates GC garbage and GameObject instantiation/destruction overhead for recurring ambient playback. A pooled/reused child emitter is more performant and predictable.

## 4. Interval Timing, State Management & Pause Handling

- **Decision**: 
  - Manage interval delays via an explicit coroutine or timer routine supporting `Time.unscaledDeltaTime` / `WaitForSecondsRealtime`.
  - Expose an explicit state enum: `AmbientState { Inactive, WaitingInterval, Playing, Paused }`.
  - Provide designer setting `pauseWithGame` (default `true`): when `Time.timeScale == 0`, pause ambient playback or timer, resuming seamlessly when unpaused.
- **Rationale**:
  - Constitution Principle III mandates explicit state transitions.
  - Kontexto UI and Audio standards require pause-safety and unscaled time compatibility.
- **Alternatives considered**:
  - *Relying solely on `WaitForSeconds` (scaled time)*: Breaks or permanently freezes when timeScale is set to 0 during pause menus or dialogue sequences.

## 5. Visual Scene Gizmos & Inspector Ergonomics

- **Decision**: 
  - Implement `OnDrawGizmosSelected` to render a wireframe boundary of the sampling volume (cyan/emerald wire) and draw a small marker at the last sampled sound point.
- **Rationale**:
  - Constitution Principle II mandates designer-centric inspector workflows. Level designers can visually confirm that the sampling bounds match the room geometry without running play mode guesswork.
