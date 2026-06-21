# Development Diary: Dialogue & Task System Refactor

This diary documents the architectural evolution of the project, the major decisions made, and the rationale behind those choices.

---

## 1. Initial State & Problem Diagnosis
**Status**: High coupling between Dialogue and Task systems.
*   **The Issue**: Scripts were dependent on specific classes (`ClientTaskManager`), requiring complex parameters for simple interactions.
*   **The Goal**: Create a "Pure" and "Isolated" architecture where systems don't need to know each other exist.

---

## 2. Milestone: Dialogue System Refactor
**Actions**: Moved from ScriptableObjects to JSON-driven data.
*   **Decision**: Implement **Auto-Loading** in `Awake`.
    *   *Why*: Reduces manual setup time in the Inspector. NPCs "become" their dialogue just by assigning a JSON file.
*   **Decision**: The **Immediate Snapshot** logic.
    *   *Why*: We initially tried a "Late Snapshot" to enable one-talk deliveries, but it risked "Intro Skips" for Giver NPCs. We prioritized **Stability and Predictability** by snapping the dialogue state the instant "E" is pressed.

---

## 3. Milestone: Task System Simplification
**Actions**: Reduced 4 statuses down to 3 (`Inactive`, `Active`, `Completed`).
*   **Decision**: The **Receipt Model** for reporting.
    *   *Why*: Solved the "Sequence Bug" where players talking to Closers before Givers broke the quest. Items now only "retire" if the Giver NPC confirms the report was accepted.
*   **Decision**: **Proactive Sync** (Giver -> Closer).
    *   *Why*: Instead of NPCs "watching" a global state, the Giver NPC pushes status updates to linked Closers. This ensures NPCs always have the correct dialogue ready *before* the player arrives.

---

## 4. Milestone: Interaction Pipeline Decoupling
**Actions**: Refactored `IInteractable` and `UniversalInteractor`.
*   **Decision**: Parameterless `Interact()`.
    *   *Why*: Total decoupling. The interactor no longer needs to pass Task Managers to a simple signpost or a door.
*   **Decision**: Support for **Multi-Component Triggering**.
    *   *Why*: Allows `DialogueManager` and `HostTaskManager` to live on the same GameObject and fire simultaneously from one button press.

---

## 5. Milestone: Modular Event Extension
**Actions**: Created `DialogueEventExtension.cs`.
*   **Decision**: Use UnityEvents for **Start** and **End** phases.
    *   *Why*: Keeps the core Dialogue System "pure" while allowing designers to trigger sounds, VFX, or Task completions through the Unity Inspector. 
*   **Decision**: The **Manual Completion** Method.
    *   *Why*: Instead of using hidden string keys (e.g., "TalkedToGuard"), we use an explicit `CompleteTask()` method call. It is more visual and less prone to typos for non-technical members.

---

## 7. Milestone: Pure Isolation & "Dumb" Systems
**Actions**: Removed cross-system code dependencies.
*   **Decision**: Replace direct calls with `UnityEvent<TaskStatus>`.
    *   *Why*: Achieved "Pure Isolation" where `HostTaskManager` no longer knows `DialogueManager` exists. All cross-system logic (e.g., "Complete Task -> Change Dialogue") is now wired in the Inspector. This makes every system 100% portable.

---

## 8. Milestone: Dialogue Playback Control
**Actions**: Added the `loopConversations` safety net.
*   **Decision**: Configurable Looping.
    *   *Why*: Prevents NPCs from looping back to their "Intro" dialogue once their story arc is finished. By defaulting to `false`, NPCs will stay on their final conversation branch (e.g., "Thanks for the help!") indefinitely unless explicitly told otherwise.

---

## 9. Milestone: Task System Robustness
**Actions**: Hardened `HostTaskManager` against common runtime errors.
*   **Decision**: Inspector Guard Clauses.
    *   *Why*: Added null checks for `TaskData` and `Objectives` to prevent crashes during scene setup.
*   **Decision**: Interaction Completion Guard.
    *   *Why*: Enforced a state check in `ReportProgress`. Once a task is `Completed`, the NPC stops processing new reports, ensuring game state remains static and predictable.

---

## 10. Milestone: Task Event & Hand-In Flow Refactor
**Actions**: Replaced the single `onStatusChanged` event with per-status events and introduced a `ReadyToComplete` state.
*   **Decision**: **Per-Status `UnityEvent` Fields** grouped under a `[Serializable] TaskEvents` class.
    *   *Why*: The original `UnityEvent<TaskStatus>` required listeners to manually branch on the passed status value, adding logic to the Inspector wiring. Separate `onInactive`, `onActive`, `onReadyToComplete`, and `onCompleted` events are self-documenting and directly wirable to the correct callback with zero branching. Wrapping them in `TaskEvents` keeps the Inspector clean with a single collapsible foldout.
*   **Decision**: The **`ReadyToComplete` Status**.
    *   *Why*: Previously, all objectives being met caused `CompleteTask()` to fire automatically. This was incompatible with a hand-in flow where the player must return to a Closer NPC to close the task. `ReadyToComplete` acts as an explicit, observable "waiting for hand-in" state, distinct from `Active` and `Completed`.
*   **Decision**: **Closer NPC as the sole task-closer**.
    *   *Why*: `Interact()` on a `Closer`-type host now calls `CompleteTask()`, which delegates to the Giver. This enforces a physical hand-in step and gives designers full control — the Closer's dialogue, position, and timing all gate the final reward, making the quest feel intentional rather than automatic.
*   **Tip**: Wire `onReadyToComplete` to a UI prompt or waypoint marker to guide the player to the Closer NPC once all objectives are done.

---

## 11. Milestone: `HostType.Both` — Self-Contained Task NPC
**Actions**: Extending `HostType` with a `Both` value to support NPCs that both give and close their own task.
*   **Decision Considered**: Splitting `HostTaskManager` into `GiverTaskManager` and `CloserTaskManager`.
    *   *Why Rejected*: Shared logic (`UpdateStatus`, `ReportProgress`, `AreObjectivesMet`, `FindGiver`, `linkedClosers`) would require a base class or duplication. An NPC acting as both would need two components on one GameObject, creating cross-component coupling that fights the existing architecture.
*   **Decision Made**: Add `Both` to the `HostType` enum.
    *   *Why*: The existing architecture is already built around one manager handling the full task lifecycle. `Both` slots in naturally — the NPC skips external delegation entirely and handles its own start and hand-in. Minimal code change, no new files, no structural risk.
*   **Lifecycle for `Both`**:
    *   `Inactive` → *(interact)* → `Active`
    *   `Active` → *(all key items reported)* → `ReadyToComplete`
    *   `ReadyToComplete` → *(interact same NPC again)* → `Completed`

---

## 12. Milestone: `HostType.Both` — Dialogue Snapshot Race Condition Fix
**Actions**: Fixed a timing bug where `Both` NPCs caused `DialogueManager` to snapshot the wrong conversation index on first interaction.
*   **The Bug**: `UniversalInteractor` calls `Interact()` on all `IInteractable` components in `GetComponents` order. If `HostTaskManager` was listed before `DialogueManager`, `StartTask()` fired first — changing the dialogue index via `onActive` — before `DialogueManager.Interact()` had a chance to snapshot the current index. Result: the NPC's intro dialogue was skipped and it jumped straight to the Active line.
*   **Attempted Fix (Rejected)**: Delegating `StartTask()` / `CompleteTask()` entirely to `DialogueEventExtension.onEnd` wiring in the Inspector.
    *   *Why Rejected*: Required re-wiring all existing Giver/Closer task templates to use the extension, creating unnecessary setup overhead and breaking established workflows.
*   **Decision Made**: **Deferred Coroutine** (`DeferredBothInteract()`).
    *   *Why*: `Interact()` on a `Both` host now launches a coroutine and returns immediately. The coroutine waits one frame (`yield return null`), then calls `StartTask()` or `CompleteTask()`. By the time the coroutine resumes, `DialogueManager.Interact()` has already executed its synchronous snapshot in the same frame — guaranteeing the correct index is captured. Zero changes to existing Giver/Closer templates.
*   **Concurrent Task Safety**: Verified that running multiple tasks simultaneously works correctly. Each `HostTaskManager` is a fully self-contained state machine with isolated `currentProgress` and `TaskData`. `KeyItemInstance` targets a specific `HostTaskManager` by reference, so there is no cross-task contamination.

---

## 14. Milestone: Dialogue Event Extension — Global Events
**Actions**: Added index-agnostic `onAnyStart` and `onAnyEnd` events to `DialogueEventExtension.cs`.
*   **Decision**: Wrap global events in a `[Serializable] GlobalEvents` class.
    *   *Why*: Mirrors the existing `IndexEvent` struct pattern. Keeps the Inspector clean with a dedicated collapsible **Global Events** foldout, clearly separated from the **Index Events** list. Designers can now wire up events that fire on *every* conversation (e.g., a universal sound effect or a UI overlay) without needing a matching index entry.

---

## 15. Milestone: `CameraReframer` — Inspector Polish & Bug Fixes
**Actions**: Refactored `CameraReframer.cs` for Inspector clarity, fixed `GetBlendDuration`, and added player movement locking.
*   **Decision**: `[Header]` and `[Tooltip]` grouping.
    *   *Why*: Fields were flat and undocumented. Grouping into **References**, **Camera**, and **Player** sections gives designers immediate context without reading the script.
*   **Decision**: Make `fadeCoroutine` private.
    *   *Why*: It is a runtime handle with no meaningful Inspector value. Exposing it created noise and invited accidental null assignments.
*   **Bug Fix**: `GetBlendDuration` always returned `2f`.
    *   *The Bug*: `blendDef` was computed from the `CinemachineBrain` but never read — `duration` was unconditionally returned as `2f`.
    *   *Fix*: Assign `blendDef.Time` to `duration` so the player material fade always syncs with the actual Cinemachine blend time.
*   **Decision**: Player movement lock via `SetInputActive`.
    *   *Why*: When `StartNPCFocus()` is called, the player should not be able to move or aim. `PlayerController.SetInputActive(false)` already handles movement, aim, and cursor state — `CameraReframer` now calls it directly, with `playerController` auto-resolved in `Awake` via the `PlayerBody` tag. `EndNPCFocus` restores input.

---

## 13. Summary of Technical Rationale (Revised)

### Stability & Purity
By moving to a "Dumb" system model, we've ensured that a bug in the Task System cannot crash the Dialogue System. The code is cleaner, more robust against null references, and follows the SOLID principle of Single Responsibility.

### Designer-Centric Workflow
The connection between tasks and narrative is now visible and configurable in the Unity Inspector. This empowers non-technical team members to "program" game flow through event linking and playback toggles, reducing the bottleneck on engineering.

