using System;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    private InputManager inputManager;

    [Header("Camera References")]
    public Transform cameraTransform;   // The main camera
    public Transform cameraPivot;       // The pivot point (child of player)
    public Transform playerTransform;   // The player target

    private Vector3 cameraFollowVelocity = Vector3.zero;

    [Header("Follow Settings")]
    public float cameraFollowSpeed = 0.3f;

    [Header("Rotation Settings")]
    public float camLookSpeed = 2f;
    public float camPivotSpeed = 2f;
    public float lookAngle;   // Yaw
    public float pivotAngle;  // Pitch
    public float minPivotAngle = -30f;
    public float maxPivotAngle = 30f;

    [Header("Camera Collision")]
    public LayerMask collisionLayer;
    private float defaultPosition;
    public float cameraCollisionOffset = 0.2f;
    public float minCollisionOffset = 0.2f;
    public float cameraCollisionRadius = 0.2f;
    private Vector3 cameraVectorPosition;
    private PlayerMovement playerMovement;
    [Header("Scope")]
    public GameObject scopeCanvas;
    public GameObject playerUI;
    public Camera mainCamera;
    private bool isScoped=false;
    private float orginalFOV=60f;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        playerMovement=FindObjectOfType<PlayerMovement>();
        inputManager = FindObjectOfType<InputManager>();
        PlayerManager player = FindObjectOfType<PlayerManager>();

        if (Camera.main != null)
            cameraTransform = Camera.main.transform;
        else
            Debug.LogError("Main Camera not found! Please tag your camera as MainCamera.");

        if (cameraTransform != null)
            defaultPosition = cameraTransform.localPosition.z;

        if (player != null)
            playerTransform = player.transform;
        else
            Debug.LogError("PlayerManager not found in the scene!");
    }

    void LateUpdate()
    {
        HandleAllCameraMovement();
    }

    public void HandleAllCameraMovement()
    {
        FollowTarget();
        RotateCamera();
        CameraCollision();
        isPlayerScoped();
    }

    private void FollowTarget()
    {
        if (playerTransform == null) return;

        Vector3 targetPosition = Vector3.SmoothDamp(
            transform.position,
            playerTransform.position,
            ref cameraFollowVelocity,
            cameraFollowSpeed
        );

        transform.position = targetPosition;
    }

    private void RotateCamera()
    {
        if (inputManager == null) return; // Safety check

        Vector3 rotation;
        Quaternion targetRotation;

        // Horizontal (Yaw)
        lookAngle += inputManager.cameraInputX * camLookSpeed;

        // Vertical (Pitch)
        pivotAngle -= inputManager.cameraInputY * camPivotSpeed; // inverted Y
        pivotAngle = Mathf.Clamp(pivotAngle, minPivotAngle, maxPivotAngle);

        // Apply yaw to root
        rotation = Vector3.zero;
        rotation.y = lookAngle;
        targetRotation = Quaternion.Euler(rotation);
        transform.rotation = targetRotation;

        // Apply pitch to pivot
        rotation = Vector3.zero;
        rotation.x = pivotAngle;
        targetRotation = Quaternion.Euler(rotation);
        cameraPivot.localRotation = targetRotation;

        if (playerMovement.isMoving == false && playerMovement.isSprinting == false)
        {
            playerTransform.rotation=Quaternion.Euler(0,lookAngle,0);
        }
    }

    private void CameraCollision()
    {
        float targetPosition = defaultPosition;
        RaycastHit hit;
        Vector3 direction = cameraTransform.position - cameraPivot.position;
        direction.Normalize();

        // Sphere cast to detect environment collisions
        if (Physics.SphereCast(
            cameraPivot.position,
            cameraCollisionRadius,
            direction,
            out hit,
            Mathf.Abs(targetPosition),
            collisionLayer,
            QueryTriggerInteraction.Ignore))
        {
            // Ignore player itself
            if (!hit.collider.CompareTag("Player"))
            {
                float distance = Vector3.Distance(cameraPivot.position, hit.point);
                targetPosition = -(distance - cameraCollisionOffset);
            }
        }

        // Prevent camera from clipping too close
        if (Mathf.Abs(targetPosition) < minCollisionOffset)
            targetPosition = -minCollisionOffset;

        // Smooth movement on Z-axis
        cameraVectorPosition = cameraTransform.localPosition;
        cameraVectorPosition.z = Mathf.Lerp(cameraTransform.localPosition.z, targetPosition, 0.2f);
        cameraTransform.localPosition = cameraVectorPosition;
    }
    public void isPlayerScoped()
    {
        if (inputManager.scopeInput)
        {
            scopeCanvas.SetActive(true);
            playerUI.SetActive(false);
            mainCamera.fieldOfView=10f;
        }
        else
        {
            scopeCanvas.SetActive(false);
            playerUI.SetActive(true);
            mainCamera.fieldOfView=orginalFOV;
        }
    }
}
