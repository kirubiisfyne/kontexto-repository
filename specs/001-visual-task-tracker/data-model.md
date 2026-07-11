# Data Model: Visual Task Tracker

## Entities

### `TaskTrackerUI` (MonoBehaviour)
Responsible for managing the visual list of tasks.
- **Fields**:
  - `TaskItemUI itemPrefab`: The prefab used for instantiating individual tasks in the list.
  - `Transform listContainer`: The UI container (e.g., VerticalLayoutGroup) where task items are spawned.
  - `Dictionary<string, TaskItemUI> activeTaskItems`: Maps a `taskId` to its visual representation so we can update/remove it quickly when completed.
- **Methods (Public, for UnityEvents)**:
  - `void InitializeTasks(List<HostTaskManager> tasks)`: Populates the list initially.
  - `void OnTaskCompleted(string taskId)`: Triggers the completion visual state on the corresponding `TaskItemUI`.
  - `void Show()`: Enables the UI.
  - `void Hide()`: Disables the UI.

### `TaskItemUI` (MonoBehaviour)
Responsible for displaying a single task's data.
- **Fields**:
  - `TMP_Text descriptionText`: Text component for the task description.
  - `GameObject checkmarkImage`: (Optional) Icon to show when completed.
- **Methods**:
  - `void Setup(string taskId, string description)`: Initializes the UI with task data.
  - `void MarkCompleted()`: Triggers the completion visual (cross out text, show checkmark, etc.).

### `LevelTaskTracker` (Existing MonoBehaviour - Modifications)
- **New Fields**:
  - `UnityEvent<List<HostTaskManager>> onTasksInitialized`: Fired when the level starts and tasks are ready.
  - `UnityEvent<string> onTaskCompletedEvent`: Fired when a specific task is completed, passing its `taskId`.
- **Modifications**:
  - In `SpawnAndRestoreTasks`, invoke `onTasksInitialized.Invoke(spawnedGivers)` after tasks are setup.
  - In `OnTaskCompleted`, invoke `onTaskCompletedEvent.Invoke(taskId)`.
