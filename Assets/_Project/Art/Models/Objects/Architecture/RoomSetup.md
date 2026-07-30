# 🚪 Room & Optimization System — Designer Setup Guide

This system dynamically enables/disables room props and opens/closes doors based on the current level configuration.

### Why We Use This System:
1. **Performance**: Props, renderers, colliders, and scripts in inactive rooms are turned **OFF** to save CPU/GPU performance.
2. **Gameplay Gating**: Doors to inactive/empty rooms stay closed, keeping players out of unpopulated spaces.

---

## 1. Organizing Your Room GameObject in the Scene

In the Unity **Hierarchy**, arrange each room into a single parent GameObject:

```text
Room_Classroom_01 (Parent GameObject)  <-- Attach [RoomController] here
  ├── Props_Container                  <-- Parent containing ALL room props & lights
  │    ├── Chairs
  │    ├── Tables
  │    ├── Blackboard
  │    └── Props/Decorations
  ├── Door_Pivot_Front                 <-- Door #1 (pivot at hinge)
  └── Door_Pivot_Side                  <-- Door #2 (optional, if room has multiple doors)
```

---

## 2. Setting Up `RoomController` on a Room

1. Select your Parent Room GameObject (e.g., `Room_Classroom_01`).
2. Click **Add Component** and add **`RoomController`**.
3. Fill out the Inspector fields:

### A. Room Identity
* **Room Id**: Enter a unique name for the room (e.g., `classroom_01`, `pub_office`, `multipurpose_hall`).  
  > ⚠️ *Make sure to double-check spelling! This ID must match what is typed in `LevelData`.*

### B. Props & Optimization
* **Props Container**: Drag the parent GameObject containing all the room's props into this slot.

### C. Doors Configuration
Place all door objects in the scene in their **DEFAULT CLOSED** rotation. The script automatically uses their starting angle as the closed state.

* **For Rooms with 1 or Multiple Doors**:
  1. Under **Doors Configuration**, set the `Doors` list size to match the number of doors (e.g. `1` or `2`).
  2. For each door:
     * **Door Transform**: Drag the door object/pivot into this slot.
     * **Open Euler Angles**: Enter the target local rotation when the door is OPEN (e.g., `X: 0, Y: 90, Z: 0` or `X: 0, Y: -90, Z: 0`).

* **For Open Rooms / Halls with NO Doors** *(e.g., Multipurpose Hall, Open Courtyard)*:
  * Simply leave the **`Doors`** list empty (Size = `0`). The system will manage prop performance without needing doors!

---

## 3. Unlocking Rooms in `LevelData`

To make a room active and accessible for a specific level or narrative day:

1. Select your **`LevelData`** ScriptableObject asset.
2. Under **Active Rooms**, expand **Active Room Ids**.
3. Add an entry and type the exact **`Room Id`** of the room (e.g. `classroom_01`).

---

## 4. What Happens in Play Mode?

When the level loads:
* ✅ **If a Room ID is listed in `LevelData`**:
  * Its `Props Container` is turned **ON** (`SetActive(true)`).
  * Its doors rotate to their specified **`Open Euler Angles`** to allow player entry.
* ❌ **If a Room ID is NOT listed in `LevelData`**:
  * Its `Props Container` stays **OFF** (`SetActive(false)`), saving GPU/CPU performance.
  * Its doors stay **CLOSED**, blocking player access.

---

## 💡 Quick Summary Checklist for Designers
- [ ] Parent room GameObject created with `RoomController` attached.
- [ ] Unique `Room Id` entered on `RoomController`.
- [ ] `Props Container` assigned.
- [ ] Doors assigned (or left empty for open halls).
- [ ] Door placed in **closed** rotation in scene; target **`Open Euler Angles`** specified.
- [ ] `Room Id` added to `activeRoomIds` in the target level's `LevelData` asset.
