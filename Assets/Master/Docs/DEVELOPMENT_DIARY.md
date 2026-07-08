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

## 16. Milestone: Level Loader & Save System
**Actions**: Created a scene-local `LevelLoader`, `LevelData` ScriptableObject, `SaveManager` utility, and fleshed out `PlayerData`.
*   **Decision**: **Scene-local `LevelLoader`** instead of a Singleton.
    *   *Why*: A scene-local script auto-fires in `Awake()` during async scene load — prefabs are instantiated before `SceneGateManager` resumes its coroutine, giving correct ordering for free. Zero changes to existing managers. Matches the "dumb systems" philosophy: the loader doesn't know about warping or transitions.
*   **Decision**: **`LevelData` ScriptableObject** per scene.
    *   *Why*: Designers create one asset per gameplay scene, populate it with task-group prefab entries (prefab + position + optional `usePrefabTransform` toggle). Visual, inspectable, and self-documenting.
*   **Decision**: **Full status chain replay** on restore (`Active → ReadyToComplete → Completed`).
    *   *Why*: Completed tasks are still spawned (not skipped), then driven through every status transition. This fires all intermediate Inspector-wired events in order (e.g., `onActive` sets dialogue index to 1, `onCompleted` sets it to 2), ensuring NPCs land on the correct post-completion dialogue.
*   **Decision**: **`taskId` on `TaskData`** as the save key.
    *   *Why*: Separate from the display `taskName`, which designers may rename. A stable identifier prevents save-file mismatches.
*   **Decision**: **Static `SaveManager`** for JSON I/O.
    *   *Why*: Pure functions, no MonoBehaviour lifecycle. Writes immediately on every task completion to `Application.persistentDataPath`.
*   **Decision**: **Code-subscribed `onCompleted` listener** for auto-saving.
    *   *Why*: Prefabs can't hold Inspector references to scene objects. `LevelLoader` adds the save listener after instantiation — invisible to designers, zero manual wiring needed.
*   **Decision**: **Two-condition level completion** (`AreAllTasksCompleted()` + explicit `CompleteLevel()` call).
    *   *Why*: Level is only marked complete when all tasks are done AND `CompleteLevel()` is explicitly called (after the outro cutscene). Prevents premature completion.
*   **Decision**: **Explicit `Retire()` for Key Items on restore**.
    *   *Why*: When `LevelLoader` restores a completed task, bypassing `Interact()` prevents false progress reports to the `HostTaskManager`. Instead, the loader explicitly calls a new `Retire()` method on all associated `KeyItemInstance` scripts. This ensures Interview NPCs update their dialogue and physical items are destroyed properly on scene load.

---

## 18. Milestone: Player Position Persistence
**Actions**: Extended `PlayerData` with position/rotation fields and wired `LevelLoader` to save and restore the player's transform.
*   **Decision**: `float[]` instead of `Vector3` for serialized fields.
    *   *Why*: `JsonUtility` serializes `Vector3` as `{"x":0,"y":0,"z":0}`, which works but is verbose. Flat `float[3]` arrays produce cleaner, more inspectable JSON (`[12.5, 0.0, -3.2]`). Conversion helpers (`SetPlayerTransform` / `GetPlayerTransform`) on `PlayerData` encapsulate the `Vector3 ↔ float[]` logic so no other script touches raw arrays.
*   **Decision**: Save position on every **task completion** and **level completion**.
    *   *Why*: These are the two moments `SaveManager.Save()` is already called. Piggy-backing on existing save triggers means zero new save points, zero new timing concerns. The player's position is always captured alongside the event that matters most.
*   **Decision**: Restore position in `LevelLoader.Awake()` **after** all task prefabs are spawned.
    *   *Why*: Task restoration fires Inspector-wired events that may move or configure the player (e.g., dialogue index changes). Restoring position last ensures the teleport is the final word on where the player stands.
*   **Decision**: Disable `CharacterController` during teleport.
    *   *Why*: Unity's `CharacterController` overrides `transform.position` writes if the controller is active. Briefly disabling it (`cc.enabled = false` → set position → `cc.enabled = true`) is the standard workaround and avoids physics fighting the restore.
*   **Decision**: Guard restore on `currentScene == sceneId`.
    *   *Why*: Prevents stale position data from a different scene being applied. If the player warps to a new scene with no save data for it, they spawn at the scene's default position (gate or placement).
*   **Decision**: Revived the dead `currentScene` field.
    *   *Why*: `PlayerData.currentScene` was declared but never written to. Now set on every save, it serves as the scene-position binding key and will support a future "Continue" flow from the main menu.

---

## 19. Milestone: Public Save/Load API & SaveGameBridge
**Actions**: Added `SaveGame()` and `LoadGame()` public methods to `LevelLoader`, created `SaveGameBridge.cs` for prefab-based UI.
*   **Decision**: Parameterless public methods on `LevelLoader`.
    *   *Why*: Unity's `UnityEvent` system (buttons, triggers, dialogue events) works best with zero-parameter methods. `SaveGame()` captures position + writes to disk; `LoadGame()` re-reads from disk + restores position. Both are directly wirable from the Inspector on any scene-local reference.
*   **Decision**: Refactor `OnTaskCompleted()` and `CompleteLevel()` to route through `SaveGame()`.
    *   *Why*: Both methods previously duplicated the same 3-line pattern (capture transform, set `currentScene`, call `SaveManager.Save`). Routing through `SaveGame()` eliminates duplication and ensures all save paths behave identically.
*   **Decision**: Separate `SaveGameBridge` MonoBehaviour for prefab-based UI.
    *   *Why*: Prefabs (e.g., a pause menu prefab) cannot hold references to scene objects. `SaveGameBridge` lives on the prefab itself and routes calls through the static `LevelLoader.Current` accessor. Null-guarded with a warning log if no `LevelLoader` exists in the current scene.
*   **Decision**: `LoadGame()` does NOT reload the scene or re-spawn task prefabs.
    *   *Why*: Task prefab spawning is `Awake()`'s responsibility during scene load. `LoadGame()` is a lightweight mid-scene refresh — it re-reads the JSON and restores the player's position only. Full scene restoration happens naturally on the next scene load.

---

## 20. Milestone: UI System Robustness & Speaker Profiles
**Actions**: Created `SpeakerDatabase.cs`, refactored `PauseMenuController.cs` and `DialogueUIManager.cs`.
*   **Decision**: Implement `SpeakerDatabase` as a ScriptableObject.
    *   *Why*: Decouples character metadata (names, portraits) from the UI code. A single asset now holds all character profiles, allowing designers to easily add or update portraits without touching the UI prefabs or scene instances.
*   **Decision**: Transition from `SetFloat("Speed")` to `SetTrigger("In"/"Out")` for UI animations.
    *   *Why*: Disabling UI GameObjects (`SetActive(false)`) resets the internal Animator state back to frame 0. Attempting to play an animation backwards from frame 0 using a negative speed float causes it to freeze. Standardized triggers ("In", "Out") combined with `ResetTrigger` explicitly flush stale triggers from memory and ensure reliable forward transitions, eliminating the "bouncing" animation bug.
*   **Decision**: Instant Pause with Realtime Coroutines.
    *   *Why*: Improved UX and player safety. Instead of waiting for the pause menu to fade in before freezing the game (leaving the player vulnerable), or freezing the game instantly but stalling standard coroutines, we instantly set `Time.timeScale = 0` and use `WaitForSecondsRealtime`. The UI Animators are set to Unscaled Time, allowing crisp menu transitions while the game world is safely frozen.

---

## 21. Milestone: UI Toolkit Text Editor Hardening
**Actions**: Refactored `TextEditorSystem` styles, implemented pointer capture failsafes, and built a custom focus bypass for `TextField`.
*   **Decision**: Strip inline UXML styles to organized `#ID` blocks in `TextEditorStyle.uss`.
    *   *Why*: Inline styles in UI Builder were causing layout overrides that fought with runtime C# logic. Moving them to USS guarantees predictable specificity.
*   **Decision**: `schedule.Execute` for `TextField.Focus()`.
    *   *Why*: In UI Toolkit, raw clicks on the padding of a `TextField` fail to focus natively and actively blur the line instead. By scheduling our focus command to fire *after* the native event resolution step, we bypass the native blur behavior and ensure the caret appears reliably on every click.
*   **Decision**: Smart Multi-Selection Retention.
    *   *Why*: Applying styles via the Ribbon (e.g. Dropdowns) steals focus. When the system forcefully pulls focus *back* to the active block, it triggered a `FocusInEvent` that wiped the multi-line selection. Added a check (`_activeBlock != newBlock`) to bypass the wipe, allowing users to stack multiple ribbon styles without losing their marquee highlight.
*   **Decision**: Failsafe Pointer Release & `picking-mode: ignore`.
    *   *Why*: Solved a silent UI Toolkit bug where interrupted drags permanently captured the pointer, breaking all future clicks. Added explicit `ReleasePointer` checks and set `picking-mode: ignore` on the marquee overlay to prevent transparent elements from swallowing raycasts.

---

## 22. Summary of Technical Rationale (Revised)

### Stability & Purity
By moving to a "Dumb" system model, we've ensured that a bug in the Task System cannot crash the Dialogue System. The code is cleaner, more robust against null references, and follows the SOLID principle of Single Responsibility.

### Designer-Centric Workflow
The connection between tasks and narrative is now visible and configurable in the Unity Inspector. This empowers non-technical team members to "program" game flow through event linking and playback toggles, reducing the bottleneck on engineering. Public `SaveGame()` and `LoadGame()` methods can be wired to any UnityEvent with zero code.

### Persistence & Level Loading
Task completion, level completion, and player position are all tracked via local JSON at `Application.persistentDataPath`. On scene load, `LevelLoader` spawns all task prefabs, fast-forwards completed ones through the full status chain, and restores the player's last saved position — ensuring both NPC dialogue state and player placement are correct on every load.

