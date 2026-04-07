using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections.Generic;

public class EventMenu : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] public GameObject MainPanel;
    [SerializeField] public GameObject SettingsPanel;
    [SerializeField] public GameObject GeneralPanel;
    [SerializeField] public GameObject VideoPanel;
    [SerializeField] public GameObject CodePanel;

    [Header("UI Elements")]
    [SerializeField] private Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private Button saveSettingsButton;
    [SerializeField] private AudioMixer audioMixer;

    [Header("Animation Settings")]
    public float scaleFactor = 1.1f;
    public float animationDuration = 0.2f;
    public float panelSwitchDelay = 0.1f;

    private Vector3 originalScale;
    private CanvasGroup settingsCanvasGroup;
    private bool isHovered = false;
    private bool isSettingsOpen = false;
    private Coroutine panelAnimationCoroutine;
    private Coroutine hoverAnimationCoroutine;

    private Resolution[] resolutions;
    private List<Resolution> filteredResolutions;
    private int currentResolutionIndex;
    private float currentVolume;
    private bool currentFullscreen;
    private float currentMouseSensitivity;

    private const float MouseSensitivityDefault = 0.6f;
    private const float MouseSensitivityMin     = 0.03f;
    private const float MouseSensitivityMax     = 1f;

    private void Start()
    {
        originalScale = transform.localScale;
        settingsCanvasGroup = SettingsPanel.GetComponent<CanvasGroup>();
        if (settingsCanvasGroup == null)
        {
            settingsCanvasGroup = SettingsPanel.AddComponent<CanvasGroup>();
        }

        MainPanel.SetActive(true);
        SettingsPanel.SetActive(false);
        GeneralPanel.SetActive(false);
        VideoPanel.SetActive(false);
        CodePanel.SetActive(false); // Добавлено: изначально скрываем CodePanel

        InitializeSettings();
        SetupUIEvents();
    }

    private void InitializeSettings()
    {
        // Инициализация разрешений
        resolutions = Screen.resolutions;
        filteredResolutions = new List<Resolution>();

        // Фильтрация основных разрешений (16:9 и 16:10)
        foreach (var res in resolutions)
        {
            float ratio = (float)res.width / res.height;
            if (ratio >= 1.6f && ratio <= 1.8f) // 16:9 ≈ 1.78, 16:10 = 1.6
            {
                filteredResolutions.Add(res);
            }
        }

        // Удаляем дубликаты
        filteredResolutions = filteredResolutions.FindAll(res =>
            res.width >= 1024 && res.height >= 768); // Минимальное разумное разрешение

        // Настройка Dropdown
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();

        for (int i = 0; i < filteredResolutions.Count; i++)
        {
            string option = filteredResolutions[i].width + " x " + filteredResolutions[i].height;
            options.Add(option);

            if (filteredResolutions[i].width == Screen.currentResolution.width &&
                filteredResolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        // Загрузка сохраненных настроек
        LoadSettings();
    }

    private void SetupUIEvents()
    {
        // События для элементов управления
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        volumeSlider.onValueChanged.AddListener(SetVolume);
        saveSettingsButton.onClick.AddListener(SaveSettings);

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.minValue = MouseSensitivityMin;
            mouseSensitivitySlider.maxValue = MouseSensitivityMax;
            mouseSensitivitySlider.onValueChanged.AddListener(SetMouseSensitivity);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (hoverAnimationCoroutine != null)
        {
            StopCoroutine(hoverAnimationCoroutine);
        }
        hoverAnimationCoroutine = StartCoroutine(AnimateHover(true));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (hoverAnimationCoroutine != null)
        {
            StopCoroutine(hoverAnimationCoroutine);
        }
        hoverAnimationCoroutine = StartCoroutine(AnimateHover(false));
    }

    private IEnumerator AnimateHover(bool isHovering)
    {
        float time = 0;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = isHovering ? originalScale * scaleFactor : originalScale;

        while (time < animationDuration)
        {
            time += Time.deltaTime;
            float t = time / animationDuration;
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        transform.localScale = targetScale;
    }

    public void OpenSettings()
    {
        if (panelAnimationCoroutine != null)
        {
            StopCoroutine(panelAnimationCoroutine);
        }
        panelAnimationCoroutine = StartCoroutine(OpenSettingsWithDelay());
    }

    private IEnumerator OpenSettingsWithDelay()
    {
        yield return new WaitForSeconds(panelSwitchDelay);

        MainPanel.SetActive(false);
        SettingsPanel.SetActive(true);
        GeneralPanel.SetActive(true);
        VideoPanel.SetActive(false);

        isSettingsOpen = true;
        StartCoroutine(AnimatePanel(true));
    }

    public void CloseSettings()
    {
        if (panelAnimationCoroutine != null)
        {
            StopCoroutine(panelAnimationCoroutine);
        }
        panelAnimationCoroutine = StartCoroutine(CloseSettingsWithDelay());
    }

    private IEnumerator CloseSettingsWithDelay()
    {
        yield return new WaitForSeconds(panelSwitchDelay);

        isSettingsOpen = false;
        StartCoroutine(AnimatePanel(false));

        // Закрываем все внутренние панели
        GeneralPanel.SetActive(false);
        VideoPanel.SetActive(false);
        CodePanel.SetActive(false);
    }

    public void OpenGeneral()
    {
        if (panelAnimationCoroutine != null)
        {
            StopCoroutine(panelAnimationCoroutine);
        }
        panelAnimationCoroutine = StartCoroutine(OpenGeneralWithDelay());
    }

    private IEnumerator OpenGeneralWithDelay()
    {
        yield return new WaitForSeconds(panelSwitchDelay / 2f);

        GeneralPanel.SetActive(true);
        VideoPanel.SetActive(false);
        CodePanel.SetActive(false); // Добавлено: скрываем CodePanel
        StartCoroutine(SwitchPanelsAnimation(GeneralPanel, VideoPanel));
    }

    public void OpenVideo()
    {
        if (panelAnimationCoroutine != null)
        {
            StopCoroutine(panelAnimationCoroutine);
        }
        panelAnimationCoroutine = StartCoroutine(OpenVideoWithDelay());
    }

    private IEnumerator OpenVideoWithDelay()
    {
        yield return new WaitForSeconds(panelSwitchDelay / 2f);

        GeneralPanel.SetActive(false);
        VideoPanel.SetActive(true);
        CodePanel.SetActive(false); // Добавлено: скрываем CodePanel
        StartCoroutine(SwitchPanelsAnimation(VideoPanel, GeneralPanel));
    }

    public void OpenCode()
    {
        if (panelAnimationCoroutine != null)
        {
            StopCoroutine(panelAnimationCoroutine);
        }
        panelAnimationCoroutine = StartCoroutine(OpenCodeWithDelay());
    }

    // ДОБАВЛЕНО: Корутина для открытия CodePanel с анимацией
    private IEnumerator OpenCodeWithDelay()
    {
        yield return new WaitForSeconds(panelSwitchDelay / 2f);

        GeneralPanel.SetActive(false);
        VideoPanel.SetActive(false);
        CodePanel.SetActive(true);
        StartCoroutine(SwitchPanelsAnimation(CodePanel, GeneralPanel));
    }

    // ДОБАВЛЕНО: Перегруженная версия для переключения с любой панели
    public void OpenCodeFromAnyPanel(GameObject currentPanel)
    {
        if (panelAnimationCoroutine != null)
        {
            StopCoroutine(panelAnimationCoroutine);
        }
        panelAnimationCoroutine = StartCoroutine(OpenCodeFromPanelWithDelay(currentPanel));
    }

    // ДОБАВЛЕНО: Универсальное открытие CodePanel из любой панели
    private IEnumerator OpenCodeFromPanelWithDelay(GameObject currentPanel)
    {
        yield return new WaitForSeconds(panelSwitchDelay / 2f);

        currentPanel.SetActive(false);
        CodePanel.SetActive(true);
        StartCoroutine(SwitchPanelsAnimation(CodePanel, currentPanel));
    }
    private IEnumerator AnimatePanel(bool isOpening)
    {
        float time = 0;
        float startAlpha = settingsCanvasGroup.alpha;
        float targetAlpha = isOpening ? 1f : 0f;

        if (isOpening)
        {
            SettingsPanel.SetActive(true);
            settingsCanvasGroup.alpha = 0f;
        }

        while (time < animationDuration)
        {
            time += Time.deltaTime;
            float t = time / animationDuration;
            settingsCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        settingsCanvasGroup.alpha = targetAlpha;

        if (!isOpening)
        {
            SettingsPanel.SetActive(false);
            MainPanel.SetActive(true);
        }
    }

    private IEnumerator SwitchPanelsAnimation(GameObject panelToShow, GameObject panelToHide)
    {
        float time = 0;
        CanvasGroup showGroup = panelToShow.GetComponent<CanvasGroup>();
        CanvasGroup hideGroup = panelToHide.GetComponent<CanvasGroup>();

        if (showGroup == null) showGroup = panelToShow.AddComponent<CanvasGroup>();
        if (hideGroup == null) hideGroup = panelToHide.AddComponent<CanvasGroup>();

        panelToShow.SetActive(true);
        showGroup.alpha = 0f;
        hideGroup.alpha = 1f;

        while (time < animationDuration / 2f)
        {
            time += Time.deltaTime;
            float t = time / (animationDuration / 2f);
            hideGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        panelToHide.SetActive(false);
        time = 0;

        while (time < animationDuration / 2f)
        {
            time += Time.deltaTime;
            float t = time / (animationDuration / 2f);
            showGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        showGroup.alpha = 1f;
    }

    public void CloseCodePanel()
    {
        if (CodePanel.activeSelf)
        {
            CodePanel.SetActive(false);
            GeneralPanel.SetActive(true); // или другая панель по умолчанию
        }
    }

    // ДОБАВЛЕНО: В методе закрытия настроек также закрываем CodePanel
    

    // Настройка разрешения
    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = filteredResolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        currentResolutionIndex = resolutionIndex;
    }

    // Настройка полноэкранного режима
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        currentFullscreen = isFullscreen;
    }

    // Настройка громкости
    public void SetVolume(float volume)
    {
        currentVolume = volume;
        AudioListener.volume = volume;
        Debug.Log($"Volume set to: {volume}");
    }

    /// <summary>
    /// Sets mouse sensitivity and immediately applies it to WASDMouseMovement if present in any loaded scene.
    /// </summary>
    public void SetMouseSensitivity(float value)
    {
        currentMouseSensitivity = value;
        WASDMouseMovement player = FindObjectOfType<WASDMouseMovement>();
        if (player != null)
            player.mouseSensitivity = value;
    }

    // Сохранение настроек
    public void SaveSettings()
    {
        PlayerPrefs.SetInt("Resolution", currentResolutionIndex);
        PlayerPrefs.SetInt("Fullscreen", currentFullscreen ? 1 : 0);
        PlayerPrefs.SetFloat("Volume", currentVolume);
        PlayerPrefs.SetFloat("MouseSensitivity", currentMouseSensitivity);
        PlayerPrefs.Save();
    }

    // Загрузка настроек
    private void LoadSettings()
    {
        // Разрешение
        if (PlayerPrefs.HasKey("Resolution"))
        {
            int savedResolution = PlayerPrefs.GetInt("Resolution");
            if (savedResolution < filteredResolutions.Count)
            {
                resolutionDropdown.value = savedResolution;
                SetResolution(savedResolution);
            }
        }

        // Полноэкранный режим
        if (PlayerPrefs.HasKey("Fullscreen"))
        {
            bool fullscreen = PlayerPrefs.GetInt("Fullscreen") == 1;
            fullscreenToggle.isOn = fullscreen;
            SetFullscreen(fullscreen);
        }

        // Громкость
        if (PlayerPrefs.HasKey("Volume"))
        {
            float volume = PlayerPrefs.GetFloat("Volume");
            volumeSlider.value = volume;
            SetVolume(volume);
        }

        // Чувствительность мыши
        float sensitivity = PlayerPrefs.HasKey("MouseSensitivity")
            ? PlayerPrefs.GetFloat("MouseSensitivity")
            : MouseSensitivityDefault;

        currentMouseSensitivity = sensitivity;
        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.value = sensitivity;

        // Применяем к игроку если уже загружен
        WASDMouseMovement player = FindObjectOfType<WASDMouseMovement>();
        if (player != null)
            player.mouseSensitivity = sensitivity;
    }

    // Анимация кнопок
    public void AnimateButtonClick(Button button)
    {
        StartCoroutine(ButtonClickAnimation(button));
    }

    private IEnumerator ButtonClickAnimation(Button button)
    {
        Vector3 originalButtonScale = button.transform.localScale;
        float time = 0;

        while (time < animationDuration / 2f)
        {
            time += Time.deltaTime;
            float t = time / (animationDuration / 2f);
            button.transform.localScale = Vector3.Lerp(originalButtonScale, originalButtonScale * 0.9f, t);
            yield return null;
        }

        time = 0;

        while (time < animationDuration / 2f)
        {
            time += Time.deltaTime;
            float t = time / (animationDuration / 2f);
            button.transform.localScale = Vector3.Lerp(originalButtonScale * 0.9f, originalButtonScale, t);
            yield return null;
        }

        button.transform.localScale = originalButtonScale;
    }

    // Утилиты для анимации кнопок
    public static IEnumerator AnimateButtonHover(Button button, float scaleFactor = 1.1f, float duration = 0.2f)
    {
        Vector3 originalScale = button.transform.localScale;
        Vector3 targetScale = originalScale * scaleFactor;
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            button.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        button.transform.localScale = targetScale;
    }

    public static IEnumerator AnimateButtonExit(Button button, float duration = 0.2f)
    {
        Vector3 currentScale = button.transform.localScale;
        Vector3 originalScale = currentScale ;
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            button.transform.localScale = Vector3.Lerp(currentScale, originalScale, t);
            yield return null;
        }

        button.transform.localScale = originalScale;
    }

    public void ContinueGame()
    {
        string lastScene = PlayerPrefs.GetString("LastScene", "");
        if (!string.IsNullOrEmpty(lastScene))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(lastScene);
        }
        else
        {
            // Нет сохраненной сцены, начать новую игру или остаться в главном меню
            Debug.Log("No saved scene found. Starting new game or waiting in main menu.");
        }
    }

    public void ClearSaveOnPlay()
    {
        PlayerPrefs.DeleteKey("LastScene");
        PlayerPrefs.Save();
        Debug.Log("Сохранение сцены удалено при запуске новой игры");
    }
}