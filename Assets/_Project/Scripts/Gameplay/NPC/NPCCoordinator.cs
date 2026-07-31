using System;
using UnityEngine;
using Master.Scripts.DialogueSystem;
using Master.Scripts.TaskSystem;

namespace Master.Scripts.NPC
{
    /// <summary>
    /// Logical conversation branches mapped to dialogue JSON indices.
    /// </summary>
    public enum DialogueBranch
    {
        QuestOffer = 0,
        InProgress = 1,
        HandIn = 2,
        Idle = 3
    }

    /// <summary>
    /// Master coordinator for NPC interactions.
    /// Connects DialogueManager and HostTaskManager into a clean, single-entry-point state machine.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DialogueManager))]
    public class NPCCoordinator : MonoBehaviour, IInteractable
    {
        [Header("System Dependencies")]
        [SerializeField] private DialogueManager dialogueManager;
        [SerializeField] private HostTaskManager taskManager;

        [Header("Conversation Branch Settings")]
        [Tooltip("Branch index played when offering a new quest.")]
        [SerializeField] private int questOfferIndex = (int)DialogueBranch.QuestOffer;

        [Tooltip("Branch index played while a quest is active but incomplete.")]
        [SerializeField] private int inProgressIndex = (int)DialogueBranch.InProgress;

        [Tooltip("Branch index played when handing in / completing a quest.")]
        [SerializeField] private int handInIndex = (int)DialogueBranch.HandIn;

        [Tooltip("Branch index played after quest completion or for non-quest NPCs.")]
        [SerializeField] private int idleIndex = (int)DialogueBranch.Idle;

        #region Unity Lifecycle

        private void Reset()
        {
            FetchDependencies();
        }

        private void Awake()
        {
            FetchDependencies();
        }

        private void FetchDependencies()
        {
            if (dialogueManager == null) dialogueManager = GetComponent<DialogueManager>();
            if (taskManager == null) taskManager = GetComponent<HostTaskManager>();
        }

        #endregion

        #region IInteractable Implementation

        /// <summary>
        /// Main interaction entry point called by UniversalInteractor.
        /// </summary>
        public void Interact()
        {
            // Ignore interaction if dialogue is actively playing
            if (DialogueManager.IsConversationActive) return;

            // Pure Dialogue NPC (No quest task attached)
            if (IsPureDialogueNPC())
            {
                PlayPureDialogue();
                return;
            }

            // Quest-enabled NPC state machine evaluation
            EvaluateAndPlayQuestInteraction();
        }

        #endregion

        #region Interaction Logic

        private bool IsPureDialogueNPC()
        {
            return taskManager == null || taskManager.task == null;
        }

        private void PlayPureDialogue()
        {
            if (dialogueManager != null)
            {
                dialogueManager.InteractDirectly();
            }
        }

        /// <summary>
        /// Evaluates current TaskStatus and HostType to select dialogue branch and post-dialogue actions.
        /// </summary>
        private void EvaluateAndPlayQuestInteraction()
        {
            HostType hostType = taskManager.hostType;
            TaskStatus status = taskManager.status;

            // 1. Handle Closer NPCs (Hand-in targets)
            if (hostType == HostType.Closer)
            {
                HandleCloserInteraction();
                return;
            }

            // 2. Handle Giver or Both NPCs
            switch (status)
            {
                case TaskStatus.Inactive:
                    HandleInactiveTask();
                    break;

                case TaskStatus.Active:
                    HandleActiveTask(hostType);
                    break;

                case TaskStatus.Completed:
                    PlayDialogueBranch(DialogueBranch.Idle);
                    break;
            }
        }

        private void HandleCloserInteraction()
        {
            if (taskManager.status == TaskStatus.Active && taskManager.IsReadyToComplete())
            {
                PlayDialogueThenExecute(DialogueBranch.HandIn, () => taskManager.CompleteTask());
            }
            else
            {
                PlayDialogueBranch(DialogueBranch.InProgress);
            }
        }

        private void HandleInactiveTask()
        {
            if (taskManager.HasUnmetPrerequisite())
            {
                // Play fallback/prerequisite locked conversation
                dialogueManager.UseIdleDialogue();
                return;
            }

            // Offer quest, start task after conversation finishes
            PlayDialogueThenExecute(DialogueBranch.QuestOffer, () => taskManager.StartTask());
        }

        private void HandleActiveTask(HostType hostType)
        {
            bool isBothHost = (hostType == HostType.Both);
            bool isReadyToHandIn = taskManager.IsReadyToComplete();

            if (isBothHost && isReadyToHandIn)
            {
                // Hand in quest to the same NPC
                PlayDialogueThenExecute(DialogueBranch.HandIn, () => taskManager.CompleteTask());
            }
            else
            {
                // Quest incomplete -> Play in-progress reminder dialogue
                PlayDialogueBranch(DialogueBranch.InProgress);
            }
        }

        #endregion

        #region Dialogue Execution Helpers

        private void PlayDialogueBranch(DialogueBranch branch)
        {
            if (dialogueManager == null) return;

            int conversationIndex = GetBranchIndex(branch);
            dialogueManager.currentConversationIndex = conversationIndex;
            dialogueManager.InteractDirectly();
        }

        private void PlayDialogueThenExecute(DialogueBranch branch, Action onDialogueEnded)
        {
            if (dialogueManager == null) return;

            int conversationIndex = GetBranchIndex(branch);
            dialogueManager.currentConversationIndex = conversationIndex;

            // One-shot safe event listener
            void HandleEnded(int endedIndex)
            {
                try
                {
                    onDialogueEnded?.Invoke();
                }
                finally
                {
                    dialogueManager.OnConversationEnded -= HandleEnded;
                }
            }

            dialogueManager.OnConversationEnded += HandleEnded;
            dialogueManager.InteractDirectly();
        }

        private int GetBranchIndex(DialogueBranch branch)
        {
            return branch switch
            {
                DialogueBranch.QuestOffer => questOfferIndex,
                DialogueBranch.InProgress => inProgressIndex,
                DialogueBranch.HandIn => handInIndex,
                DialogueBranch.Idle => idleIndex,
                _ => idleIndex
            };
        }

        #endregion
    }
}
