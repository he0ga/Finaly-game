using UnityEngine;

public class WASDMouseMovement : MonoBehaviour
{
    [Header("Настройки движения")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 100f;

    [Header("Покачивание при ходьбе")]
    public float bobAmount = 0.05f;
    public float bobSpeed = 10f;

    [Header("Ссылка на камеру персонажа")]
    public Camera playerCamera;

    private CharacterController controller;
    private float xRotation = 0f;
    private Vector3 startCameraLocalPos;
    private float bobTimer = 0f;

    private float mouseX;
    private float mouseY;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (playerCamera == null) playerCamera = Camera.main;
        startCameraLocalPos = playerCamera.transform.localPosition;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Движение по WASD
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * moveSpeed * Time.deltaTime);

        // Сохраняем движение мыши, чтобы обработать в LateUpdate
        mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Покачивание камеры при ходьбе
        bool isMoving = move.magnitude > 0.1f;
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
            bobTimer = 0f;
            Vector3 camPos = playerCamera.transform.localPosition;
            camPos.y = Mathf.Lerp(camPos.y, startCameraLocalPos.y, Time.deltaTime * bobSpeed);
            playerCamera.transform.localPosition = camPos;
        }
    }

    void LateUpdate()
    {
        // Поворот мышью (в LateUpdate для плавности)
        transform.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}
