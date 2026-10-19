# Phase 1: Quickstart & Validation Guide

## Setup

1. Open the project in Unity Editor.
2. In the Project window, select `Assets/_Project/Settings/Data/CharacterProfiles/MainSpeakerDB.asset`.
3. Locate a character profile (e.g., `Mrs. Santos` or `Kiko`). Assign an `AudioClip` (e.g. vocal cue or testing SFX like `sfx_uiPop0`) to `Voice Clips`.
4. Open a scene with dialogue (e.g. `scn_publication-office` or `scn_classroom0`).
5. Select the `DialogueUI` GameObject in the scene hierarchy.
6. Verify the `DialogueUIManager` component displays the new **Dialogue Vocal / Voice SFX** inspector fields.

---

## Validation Scenarios

### Scenario 1: Dialogue Trigger Audio
- **Action**: Walk up to an NPC (e.g., Mrs. Santos) and press `F` to initiate dialogue.
- **Expected Outcome**:
  - The dialogue window opens.
  - The "hmm, ahh!" voice SFX plays immediately.
  - Typewriter text begins rendering with letter blips.

### Scenario 2: Skipping Typewriter Text Reveal
- **Action**: While the dialogue text is actively typing out, press `F`.
- **Expected Outcome**:
  - The entire line appears instantly (`FinishTyping`).
  - Active typing blip stops.
  - The "hmm, ahh!" voice SFX (or configured skip SFX) plays cleanly.

### Scenario 3: Advancing to the Next Line ("Next Linning")
- **Action**: Once the line has finished displaying, press `F` to progress to the next line.
- **Expected Outcome**:
  - The conversation advances to the subsequent line.
  - The "hmm, ahh!" voice SFX (or configured next-line SFX) plays.
  - The new line begins typing out.

### Scenario 4: Final Line Dismissal / Dialogue End
- **Action**: On the final line of dialogue, press `F`.
- **Expected Outcome**:
  - The dialogue ends and the panel transitions out.
  - The next-line vocal cue does NOT play.

### Scenario 5: Missing Audio Graceful Fallback
- **Action**: Talk to an NPC with no custom audio clips assigned and clear `Default Voice SFX` on `DialogueUIManager`.
- **Expected Outcome**:
  - Dialogue functions normally.
  - Zero `NullReferenceException` errors logged in the console.
