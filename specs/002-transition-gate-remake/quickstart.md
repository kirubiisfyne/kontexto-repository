# Quickstart Validation Guide

## Validation Scenarios

### Scenario 1: Transitioning without a Target Gate
1. Trigger a scene warp to a gameplay scene using `SceneGateManager.Instance.StartWarp("GameplayScene", "")` via a UI button or debug console.
2. Verify that the fade-out plays flawlessly.
3. Verify that the scene loads asynchronously behind the black screen.
4. Verify that the screen fades in smoothly and the player's position is untouched (or no gate logic is forced).

### Scenario 2: Transitioning between specific Gates
1. Place two gates in Scene A (`Gate A1`, `Gate A2`) and two gates in Scene B (`Gate B1`, `Gate B2`).
2. Set `Gate A1` target to `Gate B2`.
3. Interact with `Gate A1`.
4. Verify the player spawns exactly at `Gate B2` in Scene B.
5. Verify that during the transition, there are no 1-frame stutters or skipped animations.
