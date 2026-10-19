# Tasks: NPC Dialogue Audio Feedback

**Input**: Design documents from `specs/004-npc-dialogue-audio/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `quickstart.md`
**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)

---

## Phase 1: Setup & Core Infrastructure

**Purpose**: Core audio foundation supporting direct `AudioClip` routing and speaker voice data

- [X] T001 Add direct `AudioClip` overload for `PlaySFX` in `Assets/_Project/Scripts/Core/AudioManager.cs`
- [X] T002 Extend `SpeakerProfile` in `Assets/_Project/Scripts/Gameplay/DialogueSystem/SpeakerDatabase.cs` with `voiceClips`, `voicePitch`, and `GetRandomVoiceClip()` helper

---

## Phase 2: User Story 1 - Dialogue Initiation Audio Cue (Priority: P1) 🎯 MVP

**Goal**: Play vocal SFX ("hmm, ahh!") when triggering dialogue with an NPC.

- [X] T003 [US1] Add dialogue vocal SFX configuration fields (`defaultVoiceSFX`, pitch, volume) and `PlayVoiceSFX(string speakerName)` method in `Assets/_Project/Scripts/Gameplay/DialogueSystem/DialogueUIManager.cs`
- [X] T004 [US1] Add `npcVoiceClip` override to `Assets/_Project/Scripts/Gameplay/DialogueSystem/DialogueManager.cs` and trigger vocal SFX on conversation start

---

## Phase 3: User Story 2 - Typewriter Skip Audio Feedback (Priority: P1)

**Goal**: Play vocal SFX / skip audio feedback when skipping an in-progress typewriter text reveal.

- [X] T005 [US2] Add `skipSFX` field and `PlaySkipSFX(string speakerName = null)` method in `Assets/_Project/Scripts/Gameplay/DialogueSystem/DialogueUIManager.cs`
- [X] T006 [US2] Trigger skip SFX in `DialogueUIManager.FinishTyping()` when the user skips text

---

## Phase 4: User Story 3 - Next Line Dialogue Progression Audio (Priority: P1)

**Goal**: Play vocal SFX / line progression audio when advancing from one line to the next.

- [X] T007 [US3] Add `nextLineSFX` field and `PlayNextLineSFX(string speakerName = null)` method in `Assets/_Project/Scripts/Gameplay/DialogueSystem/DialogueUIManager.cs`
- [X] T008 [US3] Wire next line progression audio in `DialogueUIManager.UpdateDialogueView()` for subsequent lines

---

## Phase 5: Documentation & Verification

- [X] T009 Document Milestone 27 in `Assets/_Project/Documents/DEVELOPMENT_DIARY.md`
- [X] T010 Verify Unity compilation and script integrity in `Editor.log`
