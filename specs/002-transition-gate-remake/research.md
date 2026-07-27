# Research: Transition and Gate System Refactor

## Decisions

### 1. Robust Animator Polling
- **Decision**: Introduce a 1-frame wait (`yield return null;`) before reading `Animator.GetCurrentAnimatorStateInfo(0).length` at initialization boundaries, or forcefully tick `animator.Update(0)`.
- **Rationale**: `Animator` state machines take one frame to evaluate their `Entry` node into the default state after being enabled. By delaying the poll, we ensure the correct animation length is retrieved, avoiding the `0` seconds wait bug that was prematurely disabling the Canvas.
- **Alternatives considered**: Replacing Animator completely with CanvasGroup alpha tweening (rejected per user request to preserve custom animation assets).

### 2. Scene Loading Coroutine
- **Decision**: Update `SceneGateManager` to utilize `AsyncOperation.allowSceneActivation = false` during the load sequence.
- **Rationale**: This holds the loaded scene in the background until the screen is fully black. Once the `TransitionManager` finishes the fade-out, `allowSceneActivation = true` is set. This avoids all pop-in.
- **Alternatives considered**: Standard synchronous `SceneManager.LoadScene` (rejected due to hard stuttering and freezing).
