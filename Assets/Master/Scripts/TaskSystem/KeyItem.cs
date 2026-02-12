using UnityEngine;

public class KeyItem : MonoBehaviour
{
    public string itemKey; // Set this to "Wood" in Inspector

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Find the manager and report the find
            TaskManager manager = other.GetComponent<TaskManager>();
            if (manager != null)
            {
                manager.ReportEvent(itemKey, 1);
                Destroy(gameObject); // Poof!
            }
        }
    }
}