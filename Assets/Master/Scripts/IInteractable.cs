using Master.Scripts.TaskSystem;
using UnityEngine;

namespace Master.Scripts
{
    public interface IInteractable
    {
        void Interact(ClientTaskManager clientTaskManager);
    }
}