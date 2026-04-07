using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PanelController : MonoBehaviour
{
    public GameObject Panel;
    public float animationDuration = 0.5f;
    public float delayBeforeTransition = 1.0f; // Время ожидания после появления панели
    private CanvasGroup canvasGroup;
    public GameObject panelfirst;

    private void Awake()
    {
        // Получаем компоненты
        canvasGroup = Panel.GetComponent<CanvasGroup>();

        // Устанавливаем начальное состояние
        Panel.SetActive(false);
        canvasGroup.alpha = 0;
    }

    public void ClickButton(bool open)
    {
        if (open)
        {
            Panel.SetActive(open);
            StartCoroutine(AnimatePanel(true));
        }
        else
        {
            StartCoroutine(AnimatePanel(false));
        }
    }

    public void LoadSceneWithPanelAnimation(string scene)
    {
        StartCoroutine(SceneTransitionWithPanel(scene));
    }

    private IEnumerator SceneTransitionWithPanel(string sceneName)
    {
        // Показываем панель с анимацией
        Panel.SetActive(true);
        yield return StartCoroutine(AnimatePanel(true));

        // Ждем указанное время после появления панели
        yield return new WaitForSeconds(delayBeforeTransition);

        // Переход на указанную сцену
        SceneManager.LoadScene(sceneName);
    }

    public void ASD(string scene)
    {
        // Альтернативный метод для обратной совместимости
        StartCoroutine(SceneTransitionWithPanel(scene));
    }


    public void ExitGame()
    {
        Application.Quit();
    }

    private IEnumerator AnimatePanel(bool isOpening)
    {
        float time = 0;
        float startAlpha = canvasGroup.alpha;
        float targetAlpha = isOpening ? 1f : 0f;

        // Анимация появления
        if (isOpening)
        {
            Panel.SetActive(true);
            canvasGroup.alpha = 0f;
        }

        while (time < animationDuration)
        {
            time += Time.deltaTime;
            float t = time / animationDuration;

            // Плавное изменение прозрачности
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        // Если это анимация исчезновения - деактивируем панель
        if (!isOpening)
        {
            Panel.SetActive(false);
        }
    }

    private IEnumerator LoadSceneAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }
}