using UnityEngine;

public class SetupInspectSystem : MonoBehaviour
{
    [Header("Setup Settings")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private bool destroyAfterSetup = true;

    private void Start()
    {
        if (autoSetupOnStart)
            SetupInspectSystemOnPlayer();
    }

    [ContextMenu("Setup Inspect System")]
    public void SetupInspectSystemOnPlayer()
    {
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        
        if (!ValidatePlayerController(playerController)) return;
        if (HasExistingInspectComponent(playerController)) return;
        
        AddInspectComponent(playerController);
        CleanupIfRequested();
    }

    private bool ValidatePlayerController(PlayerController playerController)
    {
        if (playerController == null)
        {
            Debug.LogError("[SetupInspectSystem] No PlayerController found in the scene!");
            return false;
        }
        return true;
    }

    private bool HasExistingInspectComponent(PlayerController playerController)
    {
        InspectItem existingInspect = playerController.GetComponent<InspectItem>();
        if (existingInspect != null)
        {
            Debug.Log("[SetupInspectSystem] InspectItem component already exists on player.");
            return true;
        }
        return false;
    }

    private void AddInspectComponent(PlayerController playerController)
    {
        playerController.gameObject.AddComponent<InspectItem>();
        Debug.Log($"[SetupInspectSystem] Successfully added InspectItem component to {playerController.gameObject.name}");
        Debug.Log("[SetupInspectSystem] Press 'I' while holding an object to inspect it!");
    }

    private void CleanupIfRequested()
    {
        if (destroyAfterSetup)
        {
            Debug.Log("[SetupInspectSystem] Setup complete. Destroying setup script.");
            Destroy(gameObject);
        }
    }
}
