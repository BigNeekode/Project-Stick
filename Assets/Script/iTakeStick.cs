using UnityEngine;

public class ITakeStick : MonoBehaviour, IInteractable
{
    [Header("Target Settings")]
    [SerializeField] private Transform rootTargetTransform;
    
    [Header("Animation Settings")]
    [SerializeField] private float lerpDuration = 1.0f;
    [SerializeField] private AnimationCurve lerpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private GameObject stickObject;
    private bool isMoving;
    private bool hasBeenTaken;
    private GameObject tempHandTarget;
    
    private void Awake()
    {
        stickObject = gameObject;
    }

    public void Interact()
    {
        if (hasBeenTaken || isMoving) return;
        
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (!ValidatePlayerController(playerController)) return;
        
        if (playerController.IsHoldingObject())
        {
            Debug.Log("Cannot take stick: Player is already holding an object!");
            return;
        }
        
        TakeStick(playerController);
    }

    private bool ValidatePlayerController(PlayerController playerController)
    {
        if (playerController == null)
        {
            Debug.LogError("PlayerController not found!");
            return false;
        }
        return true;
    }

    private void TakeStick(PlayerController playerController)
    {
        GenerateStickStats();
        CreateHandTarget(playerController);
        ConfigurePhysics();
        playerController.SetHeldObject(stickObject);
        StartCoroutine(LerpToTarget());
    }

    private void GenerateStickStats()
    {
        var stick = stickObject.GetComponent<Stick>();
        if (stick != null)
        {
            stick.GenerateStats();
        }
    }

    private void CreateHandTarget(PlayerController playerController)
    {
        GameObject tempTarget = new GameObject("CameraRelativeHandTarget");
        tempTarget.transform.position = playerController.GetCameraRelativeHandPosition();
        tempTarget.transform.rotation = playerController.GetHandRotation();
        rootTargetTransform = tempTarget.transform;
        tempHandTarget = tempTarget;
    }

    private void ConfigurePhysics()
    {
        var rigidbody = stickObject.GetComponent<Rigidbody>();
        var collider = stickObject.GetComponent<Collider>();
        
        if (collider != null)
            collider.enabled = false;
        
        if (rigidbody != null)
            rigidbody.isKinematic = true;
    }
    
    private System.Collections.IEnumerator LerpToTarget()
    {
        isMoving = true;
        
        Vector3 startPosition = stickObject.transform.position;
        Quaternion startRotation = stickObject.transform.rotation;
        Vector3 targetPosition = rootTargetTransform.position;
        Quaternion targetRotation = rootTargetTransform.rotation;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < lerpDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / lerpDuration;
            float curveValue = lerpCurve.Evaluate(progress);
            
            stickObject.transform.position = Vector3.Lerp(startPosition, targetPosition, curveValue);
            stickObject.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, curveValue);
            
            yield return null;
        }
        
        FinalizePosition(targetPosition, targetRotation);
    }

    private void FinalizePosition(Vector3 targetPosition, Quaternion targetRotation)
    {
        stickObject.transform.position = targetPosition;
        stickObject.transform.rotation = targetRotation;
        stickObject.transform.SetParent(rootTargetTransform);
        
        isMoving = false;
        hasBeenTaken = true;
    }
    
    public void ResetState()
    {
        hasBeenTaken = false;
        isMoving = false;
    }
    
    public void CleanupTempTarget()
    {
        if (tempHandTarget != null && tempHandTarget != gameObject && tempHandTarget != stickObject)
        {
            Destroy(tempHandTarget);
            tempHandTarget = null;
            rootTargetTransform = null;
        }
    }
}
