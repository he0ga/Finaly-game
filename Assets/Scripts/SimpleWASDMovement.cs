using UnityEngine;

public class WASDMouseMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Mouse Look Settings")]
    [Tooltip("Mouse sensitivity — frame-rate independent.")]
    public float mouseSensitivity = 0.15f;

    [Header("Head Bob Settings")]
    public float bobAmount = 0.05f;
    public float bobSpeed = 10f;

    [Header("References")]
    public Camera playerCamera;

    private CharacterController controller;
    private Rigidbody attachedRigidbody;

    private float xRotation = 0f;
    private Vector3 startCameraLocalPos;
    private float bobTimer = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Disable Rigidbody physics so it doesn't fight CharacterController
        attachedRigidbody = GetComponent<Rigidbody>();
        if (attachedRigidbody != null)
        {
            attachedRigidbody.isKinematic = true;
        }

        if (playerCamera == null) playerCamera = Camera.main;
        startCameraLocalPos = playerCamera.transform.localPosition;

        // Apply saved sensitivity from Settings if available
        if (PlayerPrefs.HasKey("MouseSensitivity"))
            mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMovement();
        HandleHeadBob();
    }

    void LateUpdate()
    {
        HandleMouseLook();
    }

    /// <summary>Moves the player using CharacterController.</summary>
    private void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * moveSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Rotates the player body and camera based on raw mouse delta.
    /// Uses raw mouse input to avoid Unity's built-in smoothing that causes jitter.
    /// </summary>
    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    /// <summary>Applies a smooth head-bob effect to the camera while moving.</summary>
    private void HandleHeadBob()
    {
        bool isMoving = controller.velocity.magnitude > 0.1f;

        if (isMoving)
        {
            bobTimer += Time.deltaTime * bobSpeed;
            float bobOffset = Mathf.Sin(bobTimer) * bobAmount;
            Vector3 camPos = playerCamera.transform.localPosition;
            camPos.y = startCameraLocalPos.y + bobOffset;
            playerCamera.transform.localPosition = camPos;
        }
        else
        {
            // Smoothly return camera to rest position — do NOT reset bobTimer to avoid snap
            Vector3 camPos = playerCamera.transform.localPosition;
            camPos.y = Mathf.Lerp(camPos.y, startCameraLocalPos.y, Time.deltaTime * bobSpeed);
            playerCamera.transform.localPosition = camPos;
        }
    }
}
