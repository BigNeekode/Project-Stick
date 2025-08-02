using UnityEngine;
using UnityEngine.Events;

public class TriggerArea : MonoBehaviour
{
    private Collider triggerCollider;
    [SerializeField] private UnityEvent onEnterEvent;
    [SerializeField] private UnityEvent onExitEvent;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null || !triggerCollider.isTrigger)
        {
            Debug.LogError("TriggerArea requires a Collider component with 'isTrigger' enabled.");
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (triggerCollider != null && other.gameObject.CompareTag("Player"))
        {
            onEnterEvent.Invoke();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (triggerCollider != null && other.gameObject.CompareTag("Player"))
        {
            onExitEvent.Invoke();
        }
    }
}