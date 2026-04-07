using System.Collections;
using UnityEngine;

public class NavigationScript : MonoBehaviour
{
    public GameObject camera; // Ссылка на камеру
    public float rotationDuration = 0.5f; // Длительность поворота
    public float rotationAngleLeft = 30f; // Угол поворота влево
    public float rotationAngleRight = 30f; // Угол поворота вправо


    private float initialYRotation = 90f; // Начальный угол поворота
    private Coroutine currentRotation; // Текущая корутина поворота
    private bool isLeftPressed = false;
    private bool isRightPressed = false;


    private void Awake()
    {
        // Устанавливаем начальное положение камеры
        camera.transform.rotation = Quaternion.Euler(0, initialYRotation, 0);
    }

    private void Update()
    {
        HandleKeyboardInput();
    }

    private void HandleKeyboardInput()
    {
        // Обработка нажатия влево
        if (Input.GetKeyDown(KeyCode.A)) isLeftPressed = true;
        if (Input.GetKeyUp(KeyCode.A)) isLeftPressed = false;

        // Обработка нажатия вправо
        if (Input.GetKeyDown(KeyCode.D)) isRightPressed = true;
        if (Input.GetKeyUp(KeyCode.D)) isRightPressed = false;



        // Определяем нужный поворот
        if (isLeftPressed && !isRightPressed)
        {
            RotateTo(-rotationAngleLeft);
        }
        else if (isRightPressed && !isLeftPressed)
        {
            RotateTo(rotationAngleRight);
        }
        else if (!isLeftPressed && !isRightPressed)
        {
            RotateTo(0); // Возврат в исходное положение
        }
    }

    public void ButtonRotate(string direction)
    {
        switch (direction)
        {
            case "left":
                isLeftPressed = true;
                isRightPressed = false;
                break;
            case "right":
                isRightPressed = true;
                isLeftPressed = false;
                break;
            case "release":
                isLeftPressed = false;
                isRightPressed = false;
                break;
        }
    }

    private void RotateTo(float targetAngleOffset)
    {
        // Останавливаем текущий поворот, если он есть
        if (currentRotation != null)
        {
            StopCoroutine(currentRotation);
        }

        // Запускаем новый поворот
        currentRotation = StartCoroutine(SmoothRotate(targetAngleOffset));
    }

    private IEnumerator SmoothRotate(float targetAngleOffset)
    {
        float time = 0;
        Quaternion startRotation = camera.transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(0, initialYRotation + targetAngleOffset, 0);

        while (time < rotationDuration)
        {
            time += Time.deltaTime;
            float t = time / rotationDuration;
            camera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        camera.transform.rotation = targetRotation;
        currentRotation = null;
    }
}