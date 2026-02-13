using UnityEngine;

namespace Master.Scripts.DialogueSystem
{
    public interface IInteractable
    {
        void Interact(GameObject player);
    }
}