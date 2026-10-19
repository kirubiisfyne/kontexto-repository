# Phase 1: Data Model & State Transitions

## 1. Entities & Structures

### `GruntSet` (ScriptableObject)

Reusable collection of vocal sound effects ("hmm, ahh!" clips) representing a gendered or styled voice type (e.g. `GruntSet_Male.asset`, `GruntSet_Female.asset`).

| Field | Type | Default | Description |
|---|---|---|---|
| `setName` | `string` | `""` | Descriptive label (e.g. 'Male', 'Female'). |
| `clips` | `AudioClip[]` | `[]` | Array of vocal audio clips. A random clip is selected per line. |
| `basePitch` | `float` (0.5-1.5) | `1.0f` | Base pitch multiplier for this set. |

---

### `SpeakerDatabase` (ScriptableObject)

| Field | Type | Default | Description |
|---|---|---|---|
| `maleGruntSet` | `GruntSet` | `null` | Default GruntSet asset for male characters. |
| `femaleGruntSet` | `GruntSet` | `null` | Default GruntSet asset for female characters. |
| `profiles` | `List<SpeakerProfile>` | `[]` | List of character profiles. |

---

### `SpeakerProfile` (in `SpeakerDatabase.cs`)

Represents character-specific data within the `SpeakerDatabase` ScriptableObject.

#### Extended Fields

| Field | Type | Default | Description |
|---|---|---|---|
| `characterName` | `string` | `""` | The name matching JSON speaker headers (e.g. 'Adviser', 'Kiko'). |
| `portraitSprite` | `Sprite` | `null` | The portrait image to display in the UI. |
| `gender` | `SpeakerGender` | `Unspecified` | Speaker gender (`Male` or `Female`) mapping to `maleGruntSet` or `femaleGruntSet`. |
| `gruntSet` | `GruntSet` | `null` | Optional character-specific `GruntSet` override. |
| `voicePitch` | `float` (0.5-1.5) | `1.0f` | Additional pitch multiplier for this speaker (stacks with GruntSet pitch). |
---

### `DialogueUIManager` (MonoBehaviour)

Central UI manager for dialogue presentation, typewriter rendering, and dialogue audio feedback.

#### Extended Inspector Serialized Fields

| Group | Field | Type | Default | Description |
|---|---|---|---|---|
| **Dialogue Vocal / Voice SFX** | `defaultVoiceSFX` | `AudioClip` | `null` | Default voice SFX ('hmm, ahh!' vocal cue) played on dialogue trigger, skip, or next line if speaker profile has no custom clip. |
| | `skipSFX` | `AudioClip` | `null` | Optional distinct SFX played when skipping text. If unassigned, falls back to the active voice clip. |
| | `nextLineSFX` | `AudioClip` | `null` | Optional distinct SFX played when advancing lines. If unassigned, falls back to the active voice clip. |
| | `dialogueCloseSFX` | `AudioClip` | `null` | Optional SFX played when the dialogue panel closes. |
| | `randomVoicePitch` | `bool` | `true` | Whether to apply slight random pitch shifting to prevent repetitive sound. |
| | `minVoicePitch` | `float` (0.8-1.2) | `0.95f` | Minimum pitch variation. |
| | `maxVoicePitch` | `float` (0.8-1.2) | `1.05f` | Maximum pitch variation. |
| | `voiceVolume` | `float` (0-1) | `0.8f` | Playback volume scale for vocal cues. |

---

### `DialogueManager` (MonoBehaviour)

Interacted NPC component controlling dialogue state and line advancement.

#### Extended Inspector Serialized Fields

| Group | Field | Type | Default | Description |
|---|---|---|---|---|
| **Audio Settings** | `npcVoiceClip` | `AudioClip` | `null` | Optional direct vocal clip override for this NPC, taking precedence over SpeakerDatabase. |

---

## 2. State Progression & Audio Triggers

```
[Player Presses F on NPC]
         │
         ▼
[Interact() -> StartDialogueRoutine()]
         │
         ├─► Play Voice SFX ("hmm, ahh!")
         │
         ▼
[Typewriter Effect Active: IsTyping == true]
         │
         ├──────────────────────────────────────────────┐
         │ (Player presses F during typing)              │ (Text completes naturally)
         ▼                                              ▼
[DisplayNextLine() -> FinishTyping()]             [IsTyping = false]
         │                                              │
         ├─► Play Voice SFX ("hmm, ahh!") / Skip SFX    │ (Player presses F)
         │                                              ▼
         └───────────────────────────────────────► [DisplayNextLine()]
                                                        │
                                    ┌───────────────────┴───────────────────┐
                                    ▼                                       ▼
                       (linesQueue.Count > 0)                    (linesQueue.Count == 0)
                                    │                                       │
                                    ├─► Play Voice SFX                      ├─► EndDialogue()
                                    │   ("hmm, ahh!")                       │   (Optional Close SFX)
                                    ▼                                       ▼
                         [UpdateDialogueView()]                    [Dialogue UI Closes]
```
