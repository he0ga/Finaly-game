using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Singleton-компонент для плавных переходов между сценами.
/// Размести на одном GameObject в любой сцене — он сохранится между загрузками.
/// Каждая новая сцена начинается с авто-проявления экрана.
/// </summary>
public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    [Header("Длительность")]
    [Tooltip("Время (сек) затухания до чёрного перед загрузкой новой сцены.")]
    [SerializeField] [Min(0.05f)] private float fadeOutDuration = 1f;
    [Tooltip("Время (сек) проявления после загрузки новой сцены.")]
    [SerializeField] [Min(0.05f)] private float fadeInDuration = 1f;

    [Header("Внешний вид")]
    [Tooltip("Цвет экрана во время перехода.")]
    [SerializeField] private Color fadeColor = Color.black;

    private Image overlay;
    private bool isTransitioning = false;

    // ── Инициализация ────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupCanvas();

        // Начинаем с чёрного экрана — авто-проявление при старте первой сцены.
        SetAlpha(1f);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void SetupCanvas()
    {
        // Canvas прямо на этом же GameObject — вместе с ним DontDestroyOnLoad.
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767; // поверх любого другого UI

        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        var overlayGO = new GameObject("FadeOverlay");
        overlayGO.transform.SetParent(transform, false);

        overlay = overlayGO.AddComponent<Image>();
        overlay.raycastTarget = false; // не мешает кликам когда прозрачный

        var rect = overlayGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        SetAlpha(1f); // устанавливаем цвет до первого рендера
    }

    // ── Публичный API ─────────────────────────────────────────────────────────

    /// <summary>Плавно затемняет экран, загружает сцену, затем плавно проявляет её.</summary>
    public void TransitionTo(string sceneName)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(sceneName));
    }

    /// <summary>Плавно затемняет экран без смены сцены. Используй совместно с LoadScene.</summary>
    public IEnumerator FadeOut()
    {
        yield return StartCoroutine(FadeTo(1f, fadeOutDuration));
    }

    /// <summary>Плавно проявляет экран из чёрного.</summary>
    public IEnumerator FadeIn()
    {
        yield return StartCoroutine(FadeTo(0f, fadeInDuration));
    }

    // ── Внутренняя логика ─────────────────────────────────────────────────────

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Гарантируем что время не застряло на 0 после победной паузы.
        Time.timeScale = 1f;

        // Если переход уже управляется TransitionRoutine — она сама запустит FadeIn.
        if (!isTransitioning)
            StartCoroutine(FadeTo(0f, fadeInDuration));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        isTransitioning = true;

        yield return StartCoroutine(FadeTo(1f, fadeOutDuration));

        // Восстанавливаем время перед загрузкой — на случай если игра была паузирована.
        Time.timeScale = 1f;

        SceneManager.LoadScene(sceneName);

        // Ждём один кадр — Unity регистрирует sceneLoaded на следующем кадре.
        yield return null;

        yield return StartCoroutine(FadeTo(0f, fadeInDuration));

        isTransitioning = false;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = overlay.color.a;
        float elapsed    = 0f;

        while (elapsed < duration)
        {
            // Используем unscaledDeltaTime — fade работает даже при Time.timeScale = 0.
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (overlay == null) return;
        overlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
    }
}
