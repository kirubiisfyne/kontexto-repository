using System.Collections.Generic;
using UnityEngine;

namespace Master.Scripts.TaskSystem
{
    public class TaskCycleManager : MonoBehaviour
    {
        [System.Serializable]
        public class TaskStakeholders
        {
            public TaskData taskData;
            public HostTaskManager taskGiver;
            public HostTaskManager taskCloser;
            public List<GameObject> objectiveKeyItems;
        }
        
        [Tooltip("Fill all fields.")]
        public TaskStakeholders stakeholders;
        public TaskStatus taskStatus = TaskStatus.NotStarted;
        
        private GameObject clientGameObject;
        private ClientTaskManager clientTaskManager;

        private void Awake()
        {
            SetupTaskSCycle();
        }

        private void SetupTaskSCycle()
        {
            stakeholders.taskGiver.heldTask = stakeholders.taskData;
            stakeholders.taskGiver.taskCycleManager = this;
            
            stakeholders.taskCloser.heldTask = stakeholders.taskData;
            stakeholders.taskCloser.taskCycleManager = this;

            foreach (GameObject objectiveKeyItem in stakeholders.objectiveKeyItems)
            {
                objectiveKeyItem.GetComponent<KeyItemInstance>().taskCycleManager = this;
            }
        }

        public void UpdateTaskStatus(TaskStatus newStatus)
        {
            taskStatus = newStatus;
            
            stakeholders.taskGiver.hostTaskStatus = taskStatus;
            stakeholders.taskCloser.hostTaskStatus = taskStatus;
            
            Debug.Log($"Task status updated to {newStatus}");
            Debug.Log($"Current task status is {taskStatus}");
            
        }
    }
}