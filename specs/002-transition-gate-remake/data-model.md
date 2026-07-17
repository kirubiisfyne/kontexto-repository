# Data Model: Transition and Gate System

## Entities

### `SceneGateInstance`
- **Fields**:
  - `sceneToName` (string): The name of the scene to load.
  - `gateId` (string): A unique identifier for this gate within the scene (e.g., "CampusFront", "DormRoom1").
  - `targetGateId` (string, optional): The ID of the gate to spawn at in the target scene. If empty, the player is not repositioned.
  - `canPlayerWarp` (bool): Whether the gate is active.
  - `triggerMode` (GateTriggerMode): Interact vs OnTriggerEnter.
  - `onWarpStart` (UnityEvent): Hook for before warp.

### `SceneGateManager`
- **Fields**:
  - `targetGateId` (string): Stores the requested target gate ID during scene transitions to inform the next scene where the player should spawn. This replaces the brittle `lastSceneString`.
