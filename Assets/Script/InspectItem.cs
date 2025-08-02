using Unity.Cinemachine;
using UnityEngine;

public class InspectItem : MonoBehaviour
{
    [Header("Inspect Settings")]
    [SerializeField] private KeyCode inspectKey = KeyCode.I;
    [SerializeField] private Vector3 inspectPosition = new Vector3(0f, 0f, 1.5f);
    [SerializeField] private float transitionSpeed = 5f;
    [SerializeField] private float rotationSpeed = 100f;
    
    [Header("Mouse Controls")]
    [SerializeField] private bool invertMouseY = false;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private CinemachineInputAxisController inputAxisController;
    
    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoomDistance = 0.5f;
    [SerializeField] private float maxZoomDistance = 3f;
    
    private PlayerController playerController;
    private Camera playerCamera;
    
    private bool isInspecting;
    private GameObject inspectedObject;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;
    private bool originalKinematic;
    
    private Vector2 mouseInput;
    private Vector3 currentRotation;
    private float currentZoomDistance;

    private void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        playerCamera = Camera.main;
        
        ValidateComponents();
        
        currentZoomDistance = inspectPosition.z;
    }

    private void Update()
    {
        HandleInspectInput();
        
        if (isInspecting)
        {
            HandleInspectControls();
            UpdateInspectedObjectPosition();
        }
    }

    private void ValidateComponents()
    {
        if (playerController == null)
            Debug.LogError("[InspectItem] PlayerController not found!");
        
        if (playerCamera == null)
            Debug.LogError("[InspectItem] Main camera not found!");
    }

    private void HandleInspectInput()
    {
        if (Input.GetKeyDown(inspectKey))
        {
            if (isInspecting)
                StopInspecting();
            else
                StartInspecting();
        }
    }

    private void StartInspecting()
    {
        if (!CanStartInspecting()) return;
        
        inspectedObject = GetHeldObject();
        if (inspectedObject == null)
        {
            Debug.LogWarning("[InspectItem] Could not get held object for inspection!");
            return;
        }
        
        StoreOriginalState();
        SetupInspectionState();
        DisablePlayerControls();
        ConfigureCursor(true);
    }

    private bool CanStartInspecting()
    {
        if (playerController == null || !playerController.IsHoldingObject())
        {
            Debug.Log("[InspectItem] No object to inspect!");
            return false;
        }
        return true;
    }

    private void StoreOriginalState()
    {
        originalPosition = inspectedObject.transform.position;
        originalRotation = inspectedObject.transform.rotation;
        originalParent = inspectedObject.transform.parent;
        
        var rigidbody = inspectedObject.GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            originalKinematic = rigidbody.isKinematic;
            rigidbody.isKinematic = true;
        }
    }

    private void SetupInspectionState()
    {
        isInspecting = true;
        currentRotation = Vector3.zero;
        currentZoomDistance = inspectPosition.z;
    }

    private void DisablePlayerControls()
    {
        if (playerController != null)
            playerController.enabled = false;
        
        if (inputAxisController != null)
            inputAxisController.enabled = false;
    }

    private void ConfigureCursor(bool lockCursor)
    {
        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockCursor;
    }

    private void StopInspecting()
    {
        if (!isInspecting || inspectedObject == null) return;
        
        RestoreOriginalState();
        ResetInspectionState();
        EnablePlayerControls();
        ConfigureCursor(false);
    }

    private void RestoreOriginalState()
    {
        inspectedObject.transform.position = originalPosition;
        inspectedObject.transform.rotation = originalRotation;
        inspectedObject.transform.SetParent(originalParent);
        
        var rigidbody = inspectedObject.GetComponent<Rigidbody>();
        if (rigidbody != null)
            rigidbody.isKinematic = originalKinematic;
    }

    private void ResetInspectionState()
    {
        isInspecting = false;
        inspectedObject = null;
    }

    private void EnablePlayerControls()
    {
        if (playerController != null)
            playerController.enabled = true;

        if (inputAxisController != null)
            inputAxisController.enabled = true;
    }

    private void HandleInspectControls()
    {
        HandleMouseInput();
        HandleZoomInput();
        ApplyRotation();
    }

    private void HandleMouseInput()
    {
        mouseInput.x = Input.GetAxis("Mouse X") * mouseSensitivity;
        mouseInput.y = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        if (invertMouseY)
            mouseInput.y = -mouseInput.y;
    }

    private void HandleZoomInput()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0f)
        {
            currentZoomDistance -= scrollInput * zoomSpeed;
            currentZoomDistance = Mathf.Clamp(currentZoomDistance, minZoomDistance, maxZoomDistance);
        }
    }

    private void ApplyRotation()
    {
        currentRotation.y += mouseInput.x * rotationSpeed * Time.deltaTime;
        currentRotation.x -= mouseInput.y * rotationSpeed * Time.deltaTime;
        
        currentRotation.x = Mathf.Clamp(currentRotation.x, -90f, 90f);
        
        if (inspectedObject != null)
            inspectedObject.transform.rotation = Quaternion.Euler(currentRotation);
    }

    private void UpdateInspectedObjectPosition()
    {
        if (inspectedObject == null || playerCamera == null) return;
        
        Vector3 targetPosition = CalculateInspectPosition();
        
        inspectedObject.transform.position = Vector3.Lerp(
            inspectedObject.transform.position, 
            targetPosition, 
            transitionSpeed * Time.deltaTime
        );
        
        if (inspectedObject.transform.parent != null)
            inspectedObject.transform.SetParent(null);
    }

    private Vector3 CalculateInspectPosition()
    {
        Vector3 zoomedPosition = new Vector3(inspectPosition.x, inspectPosition.y, currentZoomDistance);
        return playerCamera.transform.position + playerCamera.transform.TransformDirection(zoomedPosition);
    }

    private GameObject GetHeldObject()
    {
        return playerController?.GetHeldObject();
    }

    private void OnDisable()
    {
        if (isInspecting)
            StopInspecting();
    }
}
