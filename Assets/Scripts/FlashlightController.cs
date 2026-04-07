using UnityEngine;
using UnityEngine.UI;

public class CameraFlashlightSystem : MonoBehaviour
{
    [Header("Список камер (для света)")]
    public Camera[] cameras; // Камеры, которые рендерят в Render Texture
    public Light[] cameraLights; // Света, привязанные к этим камерам

    [Header("UI элементы")]
    public Button flashlightButton; // Кнопка фонарика
    public Image flashlightIcon; // Иконка кнопки (опционально)
    public Sprite lightOnIcon; // Иконка включенного света
    public Sprite lightOffIcon; // Иконка выключенного света

    private int currentActiveCameraIndex = -1; // -1 = нет активной камеры

    private void Start()
    {
        // Скрыть курсор и ограничить область его перемещения окном игры
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;

        // Выключаем все света при старте
        SetAllLights(false);

        // Назначаем метод на кнопку фонарика
        if (flashlightButton != null)
            flashlightButton.onClick.AddListener(ToggleFlashlight);

        UpdateButtonIcon();
    }


    // Вызывается при переключении камеры (назначьте этот метод на кнопки камер!)
    public void SwitchCamera(int cameraIndex)
    {
        // Если камера не существует, выходим
        if (cameraIndex < 0 || cameraIndex >= cameras.Length)
            return;

        // Выключаем свет предыдущей камеры
        if (currentActiveCameraIndex != -1)
        {
            cameraLights[currentActiveCameraIndex].enabled = false;
        }

        // Обновляем текущую камеру
        currentActiveCameraIndex = cameraIndex;
        UpdateButtonIcon();
    }

    // Включение/выключение света текущей камеры
    public void ToggleFlashlight()
    {
        if (currentActiveCameraIndex == -1) return;

        bool newState = !cameraLights[currentActiveCameraIndex].enabled;
        cameraLights[currentActiveCameraIndex].enabled = newState;
        UpdateButtonIcon();
    }

    // Выключает все света
    private void SetAllLights(bool state)
    {
        foreach (Light light in cameraLights)
        {
            if (light != null)
                light.enabled = state;
        }
    }

    // Обновляет иконку кнопки
    private void UpdateButtonIcon()
    {
        if (flashlightIcon == null) return;

        bool isLightOn = currentActiveCameraIndex != -1 &&
                         cameraLights[currentActiveCameraIndex].enabled;

        flashlightIcon.sprite = isLightOn ? lightOnIcon : lightOffIcon;
    }
}