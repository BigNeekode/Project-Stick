using UnityEngine;

public interface IInteractable
{
    void Interact();
}

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 5.0f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRange = 2.0f;
    [SerializeField] private LayerMask interactionLayerMask = -1;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private KeyCode dropKey = KeyCode.G;
    
    [Header("Inventory Settings")]
    [SerializeField] private Transform handTransform;
    [SerializeField] private Vector3 cameraRelativeHandOffset = new Vector3(0.2f, -0.3f, 0.4f);
    [SerializeField] private Vector3 swordGripRotationOffset = new Vector3(-10f, 0f, 0f);
    
    [Header("Outline Highlight Settings")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float highlightWidth = 5.0f;
    
    [Header("Debug Settings")]
    [SerializeField] private bool showDebugLogs = true;

    private CharacterController characterController;
    private Camera playerCamera;
    private Vector2 movementInput;
    private Vector3 velocity;
    private bool isGrounded;
    
    private IInteractable currentInteractable;
    private GameObject currentHoveredObject;
    
    private GameObject heldObject;
    private bool isHoldingObject;
    
    private Outline currentOutline;

    private void Start()
    {
        InitializeComponents();
    }

    private void Update()
    {
        HandleInput();
        HandleMovement();
        HandleGroundCheck();
        UpdateHeldObjectPosition();
        HandleInteraction();
        HandleDropInput();
    }

    private void InitializeComponents()
    {
        characterController = GetComponent<CharacterController>();
        playerCamera = Camera.main;
        
        ValidateComponents();
        
        if (showDebugLogs)
            Debug.Log("[PlayerController] Initialized successfully");
    }

    private void ValidateComponents()
    {
        if (characterController == null)
            Debug.LogError("[PlayerController] CharacterController component not found!");
        
        if (playerCamera == null)
            Debug.LogError("[PlayerController] Main camera not found. Make sure your Cinemachine camera is tagged as 'MainCamera'.");
        
        if (handTransform == null)
            Debug.LogWarning("[PlayerController] Hand transform not assigned. Objects will be held at player position.");
    }

    private void HandleInput()
    {
        movementInput.x = Input.GetAxis("Horizontal");
        movementInput.y = Input.GetAxis("Vertical");
    }

    private void HandleDropInput()
    {
        if (Input.GetKeyDown(dropKey) && isHoldingObject)
            DropHeldObject();
    }

    private void HandleMovement()
    {
        if (characterController == null || playerCamera == null) return;

        Vector3 inputDirection = GetCameraRelativeDirection(movementInput);
        Vector3 movement = inputDirection * speed * Time.deltaTime;
        characterController.Move(movement);
        
        ApplyGravity();
    }

    private void ApplyGravity()
    {
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;
        
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    private Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        if (playerCamera == null) return Vector3.zero;
        
        Vector3 cameraForward = playerCamera.transform.forward;
        Vector3 cameraRight = playerCamera.transform.right;
        
        cameraForward.y = 0;
        cameraRight.y = 0;
        
        cameraForward.Normalize();
        cameraRight.Normalize();
        
        return cameraForward * input.y + cameraRight * input.x;
    }

    private void HandleGroundCheck()
    {
        if (characterController == null) return;
        
        isGrounded = characterController.isGrounded;
        
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        isGrounded = isGrounded || Physics.Raycast(rayStart, Vector3.down, groundCheckDistance + 0.1f);
    }

    private void UpdateHeldObjectPosition()
    {
        if (isHoldingObject && heldObject != null)
        {
            Vector3 targetPosition = GetCameraRelativeHandPosition();
            Quaternion targetRotation = GetHandRotation();

            heldObject.transform.position = Vector3.Lerp(heldObject.transform.position, targetPosition, Time.deltaTime * 10f);
            heldObject.transform.rotation = Quaternion.Lerp(heldObject.transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    public Vector3 GetCameraRelativeHandPosition()
    {
        if (playerCamera == null)
            return handTransform != null ? handTransform.position : transform.position;

        Vector3 cameraPosition = playerCamera.transform.position;
        Vector3 cameraForward = playerCamera.transform.forward;
        Vector3 cameraRight = playerCamera.transform.right;
        Vector3 cameraUp = playerCamera.transform.up;

        return cameraPosition +
               cameraRight * cameraRelativeHandOffset.x +
               cameraUp * cameraRelativeHandOffset.y +
               cameraForward * cameraRelativeHandOffset.z;
    }

    public Quaternion GetHandRotation()
    {
        if (playerCamera == null)
            return handTransform != null ? handTransform.rotation : transform.rotation;

        Quaternion baseRotation = Quaternion.LookRotation(playerCamera.transform.forward, Vector3.up);
        Quaternion offsetRotation = Quaternion.Euler(swordGripRotationOffset);
        
        return baseRotation * offsetRotation;
    }

    private Transform GetHandTransform()
    {
        return handTransform != null ? handTransform : transform;
    }

    private void HandleInteraction()
    {
        DetectInteractable();
        
        if (Input.GetKeyDown(interactionKey))
        {
            if (currentInteractable != null)
            {
                if (showDebugLogs)
                    Debug.Log($"[PlayerController] Interacting with: {currentHoveredObject?.name}");
                
                currentInteractable.Interact();
            }
            else if (showDebugLogs)
            {
                Debug.Log("[PlayerController] No interactable object in range");
            }
        }
    }

    private void DetectInteractable()
    {
        if (playerCamera == null) return;

        Vector3 rayOrigin = playerCamera.transform.position;
        Vector3 rayDirection = playerCamera.transform.forward;

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, interactionRange, interactionLayerMask))
        {
            ProcessRaycastHit(hit);
        }
        else
        {
            ClearCurrentInteraction();
        }
    }

    private void ProcessRaycastHit(RaycastHit hit)
    {
        IInteractable interactable = hit.collider.GetComponent<IInteractable>();
        GameObject hitObject = hit.collider.gameObject;

        if (interactable != null)
        {
            if (currentInteractable != interactable)
            {
                ClearCurrentHighlight();
                
                currentInteractable = interactable;
                currentHoveredObject = hitObject;
                
                AddHighlightToObject(hitObject);
                
                if (showDebugLogs)
                    Debug.Log($"[PlayerController] Detected interactable: {hitObject.name}");
            }
        }
        else
        {
            ClearCurrentInteraction();
        }
    }

    private void ClearCurrentInteraction()
    {
        if (currentInteractable != null)
        {
            ClearCurrentHighlight();
            currentInteractable = null;
            currentHoveredObject = null;
            
            if (showDebugLogs)
                Debug.Log("[PlayerController] Cleared current interactable");
        }
    }

    public void SetHeldObject(GameObject obj)
    {
        heldObject = obj;
        isHoldingObject = obj != null;

        if (isHoldingObject)
        {
            ConfigureHeldObject();
            
            if (showDebugLogs)
                Debug.Log($"[PlayerController] Now holding: {heldObject.name}");
        }
        else if (showDebugLogs)
        {
            Debug.Log("[PlayerController] No longer holding any object");
        }
    }

    private void ConfigureHeldObject()
    {
        var rigidbody = heldObject.GetComponent<Rigidbody>();
        if (rigidbody != null)
            rigidbody.isKinematic = true;

        if (handTransform != null)
            heldObject.transform.SetParent(handTransform);
    }

    public bool IsHoldingObject()
    {
        return isHoldingObject;
    }
    
    public GameObject GetHeldObject()
    {
        return heldObject;
    }

    private void DropHeldObject()
    {
        if (!isHoldingObject || heldObject == null) return;

        if (showDebugLogs)
            Debug.Log($"[PlayerController] Dropping: {heldObject.name}");

        GameObject objectToDrop = heldObject;
        ClearHeldObjectReference();
        RestorePhysicsAndPosition(objectToDrop);
    }

    private void ClearHeldObjectReference()
    {
        heldObject = null;
        isHoldingObject = false;
    }

    private void RestorePhysicsAndPosition(GameObject objectToDrop)
    {
        objectToDrop.transform.SetParent(null);

        var rigidbody = objectToDrop.GetComponent<Rigidbody>();
        var collider = objectToDrop.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = true;
        if (rigidbody != null)
        {
            rigidbody.isKinematic = false;

            if (playerCamera != null)
            {
                Vector3 dropForce = playerCamera.transform.forward * 3f + Vector3.up * 1f;
                rigidbody.AddForce(dropForce, ForceMode.VelocityChange);
            }
        }

        if (playerCamera != null)
        {
            Vector3 dropPosition = playerCamera.transform.position + playerCamera.transform.forward * 1.5f;
            objectToDrop.transform.position = dropPosition;
        }
        
        var takeStick = objectToDrop.GetComponent<ITakeStick>();
        if (takeStick != null)
        {
            takeStick.ResetState();
            takeStick.CleanupTempTarget();
        }
    }

    private void AddHighlightToObject(GameObject obj)
    {
        if (obj == null) return;

        var outline = obj.GetComponent<Outline>();
        
        if (outline == null)
            outline = obj.AddComponent<Outline>();

        outline.OutlineColor = highlightColor;
        outline.OutlineWidth = highlightWidth;
        outline.enabled = true;

        currentOutline = outline;

        if (showDebugLogs)
            Debug.Log($"[PlayerController] Added highlight to: {obj.name}");
    }

    private void ClearCurrentHighlight()
    {
        if (currentOutline != null)
        {
            if (showDebugLogs)
                Debug.Log($"[PlayerController] Removed highlight from: {currentOutline.gameObject.name}");

            currentOutline.enabled = false;
            currentOutline = null;
        }
    }
}
