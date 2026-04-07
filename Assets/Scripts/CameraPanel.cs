using System.Collections;
using UnityEngine;

public class CameraPanel : MonoBehaviour
{
    public GameObject Panel; // Ссылка на панель
    public GameObject Panelnavigation;
    public float animationDuration = 0.5f; // Длительность анимации
    private CanvasGroup canvasGroup; // CanvasGroup для управления прозрачностью
    private RectTransform panelRectTransform; // RectTransform для управления положением панели

    // Позиции для анимации
    public float startYPosition = -500f; // Начальная позиция (снизу)
    public float endYPosition = 0f; // Конечная позиция (вверху)

    private void Awake()
    {
        // Получаем компоненты
        canvasGroup = Panel.GetComponent<CanvasGroup>();
        panelRectTransform = Panel.GetComponent<RectTransform>();

        // Устанавливаем начальное состояние
        Panel.SetActive(false);
        canvasGroup.alpha = 0; // Начальная прозрачность
        panelRectTransform.anchoredPosition = new Vector2(0, startYPosition); // Начальная позиция
    }

    public void ClickButton()
    {
        // Проверяем, открыта ли панель, и вызываем соответствующий метод
        if (Panel.activeSelf)
        {
            StartCoroutine(AnimatePanel(false)); // Закрытие панели
            Panelnavigation.SetActive(true);
        }
        else
        {
            Panel.SetActive(true);
            StartCoroutine(AnimatePanel(true)); // Открытие панели
            Panelnavigation.SetActive(false);
        }
    }

    private IEnumerator AnimatePanel(bool isOpening)
    {
        float time = 0;

        // Анимация появления
        if (isOpening)
        {
            // Устанавливаем начальное положение панели
            panelRectTransform.anchoredPosition = new Vector2(0, startYPosition);

            while (time < animationDuration)
            {
                time += Time.deltaTime;
                float t = time / animationDuration;

                // Плавное изменение прозрачности
                canvasGroup.alpha = Mathf.Lerp(0, 1, t);
                // Плавное изменение позиции
                panelRectTransform.anchoredPosition = new Vector2(0, Mathf.Lerp(startYPosition, endYPosition, t));
                yield return null;
            }
        }
        // Анимация исчезновения
        else
        {
            while (time < animationDuration)
            {
                time += Time.deltaTime;
                float t = time / animationDuration;

                // Плавное изменение прозрачности
                canvasGroup.alpha = Mathf.Lerp(1, 0, t);
                // Плавное изменение позиции
                panelRectTransform.anchoredPosition = new Vector2(0, Mathf.Lerp(endYPosition, startYPosition, t));
                yield return null;
            }

            Panel.SetActive(false); // Деактивируем панель после анимации
        }

        // Устанавливаем окончательные значения
        canvasGroup.alpha = isOpening ? 1 : 0;
        panelRectTransform.anchoredPosition = isOpening ? new Vector2(0, endYPosition) : new Vector2(0, startYPosition);
    }
}