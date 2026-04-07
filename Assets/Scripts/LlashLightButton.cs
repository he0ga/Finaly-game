using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("Flashlight Settings")]
    public Light flashlight;
    public float flickerFrequency = 0.1f;
    public float rotationSpeed = 15f;
    public float maxDistance = 50f; // Оптимальная дистанция для определения направления

    [Header("Audio")]
    public AudioSource toggleSound;
    public AudioSource loopSound;

    private bool isFlashlightOn = false;
    private float baseIntensity;
    private float flickerTimer = 0f;

    void Start()
    {
        if (flashlight != null)
        {
            baseIntensity = flashlight.intensity;
            flashlight.intensity = 0f;
            loopSound.Stop();
        }
    }

    void Update()
    {
        HandleFlashlightToggle();
        UpdateFlashlightState();
    }

    void HandleFlashlightToggle()
    {
        bool mouseButtonHeld = Input.GetMouseButton(0);

        if (mouseButtonHeld != isFlashlightOn)
        {
            isFlashlightOn = mouseButtonHeld;
            toggleSound.Play();

            if (isFlashlightOn)
                loopSound.Play();
            else
                loopSound.Stop();
        }
    }

    void UpdateFlashlightState()
    {
        if (flashlight == null) return;

        if (isFlashlightOn)
        {
            RotateTowardsMouse();
            ApplyFlickerEffect();
        }
        else
        {
            flashlight.intensity = 0f;
        }
    }

    void RotateTowardsMouse()
    {
        // Получаем позицию курсора с фиксированной глубиной
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = maxDistance; // Фиксированное расстояние от камеры
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);

        // Вычисляем направление
        Vector3 direction = (worldPosition - flashlight.transform.position).normalized;

        // Плавный поворот
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        flashlight.transform.rotation = Quaternion.Slerp(
            flashlight.transform.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );
    }

    void ApplyFlickerEffect()
    {
        flickerTimer -= Time.deltaTime;
        if (flickerTimer <= 0f)
        {
            flashlight.intensity = baseIntensity * Random.Range(0.8f, 1.2f);
            flickerTimer = flickerFrequency;
        }
    }
}