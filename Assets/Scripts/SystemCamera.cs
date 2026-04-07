using UnityEngine;
using UnityEngine.UI;

public class SystemCamera : MonoBehaviour
{
    public Camera[] cameras; // Массив камер
    public RawImage cameraDisplay; // Панель для отображения текущей камеры
    public Button[] cameraButtons; // Кнопки для переключения камер

    private int currentCameraIndex = 0; // Индекс текущей камеры

    void Start()
    {
        // Инициализация: отключить все камеры, кроме первой
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].enabled = (i == currentCameraIndex);
        }

        // Обновить отображение текущей камеры
        UpdateCameraDisplay();

        // Назначить обработчики событий для кнопок
        for (int i = 0; i < cameraButtons.Length; i++)
        {
            int index = i; // Локальная переменная для замыкания
            cameraButtons[i].onClick.AddListener(() => SwitchCamera(index));
        }
    }

    // Метод для переключения камеры
    void SwitchCamera(int newCameraIndex)
    {
        if (newCameraIndex < 0 || newCameraIndex >= cameras.Length)
            return;

        // Отключить текущую камеру
        cameras[currentCameraIndex].enabled = false;

        // Включить новую камеру
        currentCameraIndex = newCameraIndex;
        cameras[currentCameraIndex].enabled = true;

        // Обновить отображение
        UpdateCameraDisplay();
    }

    // Метод для обновления отображения текущей камеры
    void UpdateCameraDisplay()
    {
        if (cameraDisplay != null && cameras[currentCameraIndex].targetTexture != null)
        {
            cameraDisplay.texture = cameras[currentCameraIndex].targetTexture;
        }
    }
}