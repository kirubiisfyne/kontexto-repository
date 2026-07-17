# Feature Specification: Transition and Gate System Refactor

**Feature Branch**: `feature/transition-gate-remake`

**Created**: 2026-07-17

**Status**: Draft

**Input**: User description: "Refactor the transition and gate system. Make the Animator-based TransitionManager robust against 1-frame initialization bugs and race conditions while preserving existing animation assets. Replace string-based scene matching in SceneGateManager with explicit GateIDs to allow multiple gates per scene. Ensure scenes can still be loaded without requiring a SceneGateInstance prefab destination."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Robust Animator Transitions (Priority: P1)

As a player, I want smooth and reliable fade transitions when moving between scenes or menus, utilizing the custom animation assets without stuttering or hanging on black screens.

**Why this priority**: Core game flow relies on reliable transitions. If a transition breaks, the game freezes.

**Independent Test**: Can be fully tested by triggering a transition in an isolated test scene. The fade-in and fade-out should execute flawlessly using the Animator.

**Acceptance Scenarios**:

1. **Given** the game triggers a transition, **When** the transition starts, **Then** the Animator plays the correct animation clip reliably without skipping due to 1-frame initialization delays.
2. **Given** the screen is fully black, **When** the transition completes, **Then** the game proceeds and the screen fades back in cleanly.

---

### User Story 2 - Gate ID Based Spawning (Priority: P1)

As a player, I want to always spawn at the correct door or gate when entering a new scene, even if there are multiple entrances from the same previous scene.

**Why this priority**: Prevents players from spawning at the wrong location or getting stuck if scenes have complex layouts.

**Independent Test**: Can be tested by placing two gates connecting Scene A and Scene B, and ensuring the player spawns at the correct corresponding gate.

**Acceptance Scenarios**:

1. **Given** the player interacts with Gate X (which targets Gate Y), **When** the scene loads, **Then** the player spawns exactly at Gate Y.

---

### User Story 3 - Flexible Scene Loading (Priority: P2)

As a developer, I want to warp players to scenes that don't necessarily have Gate Instance prefabs (like the Main Menu), and I want the loading process to be smooth.

**Why this priority**: Ensures the system remains flexible for UI-driven scene changes and prevents crashes when Gate prefabs aren't present.

**Independent Test**: Test warping to a scene (e.g., Main Menu) with an empty Target Gate ID and ensure it loads cleanly without throwing missing prefab errors.

**Acceptance Scenarios**:

1. **Given** the system initiates a warp with no Target Gate ID, **When** the scene loads, **Then** the scene loads successfully and the player is not moved (or no player is required).
2. **Given** the player is warping to a new scene, **When** the async load operation occurs, **Then** the scene is not activated until the fade-out is complete and the background loading finishes.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: TransitionManager MUST continue using the `Animator` component, but implement robust state evaluation to prevent length calculation race conditions.
- **FR-002**: SceneGateManager MUST use unique GateIDs (e.g., strings) to identify gates instead of relying on the previous scene's name.
- **FR-003**: SceneGateInstance MUST define both its own `GateID` and an optional `TargetGateID` for warping.
- **FR-004**: SceneGateManager MUST gracefully handle empty `TargetGateID`s by loading the scene without attempting to teleport the player to a gate.
- **FR-005**: Scene loading MUST utilize `AsyncOperation.allowSceneActivation` to ensure the new scene remains hidden until the transition out is fully complete.

### Key Entities

- **Gate ID System**: An identifier (string) used by gates to uniquely identify themselves and their targets within scenes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Transition Animator clips play predictably 100% of the time, without skipping due to initialization bugs.
- **SC-002**: Players correctly spawn at the targeted gate 100% of the time when a Target Gate ID is provided.
- **SC-003**: Scenes without Gate Instances load 100% successfully without throwing reference errors.
- **SC-004**: Loading a scene does not cause visual pop-in or stuttering of the fade effect itself.

## Assumptions

- We are updating existing scenes to assign new Gate IDs to their existing SceneGateInstances.
- Animation assets (clips, controllers) already exist and will be reused without modification.
