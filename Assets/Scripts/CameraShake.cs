using UnityEngine;

public class CameraBreathingEffect : MonoBehaviour
{
    [Header("Breathing Settings")]
    [SerializeField] private float breathingSpeed = 0.5f;
    [SerializeField] private float verticalAmount = 0.05f;
    [SerializeField] private float zoomAmount = 0.03f;

    [Header("Mouse Reaction Settings")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float mouseSmoothness = 5f;
    [SerializeField] private float maxMouseOffset = 0.3f;

    [Header("Randomness")]
    [SerializeField] private float speedVariation = 0.1f;
    [SerializeField] private float intervalVariation = 2f;

    private Vector3 originalPosition;
    private float originalOrthoSize;
    private float baseSpeed;
    private float timer;
    private float currentInterval;
    private Camera cam;
    private Vector3 mouseInput;
    private Vector3 smoothMouseOffset;
    private Vector3 targetMouseOffset;

    private void Start()
    {
        cam = GetComponent<Camera>();
        originalPosition = transform.localPosition;

        if (cam.orthographic)
        {
            originalOrthoSize = cam.orthographicSize;
        }

        baseSpeed = breathingSpeed;
        ResetInterval();
         // Курсор не выходит за пределы окна
    }

    private void Update()
    {
        HandleBreathingEffect();
        HandleMouseReaction();
    }

    private void HandleBreathingEffect()
    {
        timer += Time.deltaTime;

        if (timer > currentInterval)
        {
            timer = 0f;
            ResetInterval();
        }

        float breathingEffect = Mathf.Sin(Time.time * breathingSpeed);

        Vector3 newPos = originalPosition;
        newPos.y += breathingEffect * verticalAmount;

        if (!cam.orthographic)
        {
            newPos.z += breathingEffect * zoomAmount * 0.5f;
        }

        transform.localPosition = newPos + smoothMouseOffset;

        if (cam.orthographic)
        {
            cam.orthographicSize = originalOrthoSize + breathingEffect * zoomAmount;
        }
    }

    private void HandleMouseReaction()
    {
        // Получаем положение мыши в координатах экрана (от 0 до 1)
        Vector3 mouseViewportPos = cam.ScreenToViewportPoint(Input.mousePosition);

        // Переводим в диапазон -1 до 1 (центр экрана - 0,0)
        mouseInput.x = (mouseViewportPos.x - 0.5f) * 2f;
        mouseInput.y = (mouseViewportPos.y - 0.5f) * 2f;

        // Ограничиваем входные значения
        mouseInput.x = Mathf.Clamp(mouseInput.x, -1f, 1f);
        mouseInput.y = Mathf.Clamp(mouseInput.y, -1f, 1f);

        // Вычисляем целевое смещение
        targetMouseOffset = new Vector3(
            mouseInput.x * maxMouseOffset,
            mouseInput.y * maxMouseOffset * 0.5f, // Вертикальное смещение делаем меньше
            0
        );

        // Плавно интерполируем к целевому смещению
        smoothMouseOffset = Vector3.Lerp(
            smoothMouseOffset,
            targetMouseOffset,
            mouseSmoothness * Time.deltaTime
        );
    }

    private void ResetInterval()
    {
        breathingSpeed = baseSpeed + Random.Range(-speedVariation, speedVariation);
        currentInterval = Random.Range(intervalVariation * 0.5f, intervalVariation * 1.5f);
    }
}