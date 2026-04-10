using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Plays a sequence of intro videos before showing the main menu.
/// Hold E for <see cref="skipHoldDuration"/> seconds to skip.
/// </summary>
public class IntroPlayer : MonoBehaviour
{
    [Header("Video Clips")]
    [Tooltip("Clips played in order before the menu is shown.")]
    public VideoClip[] introClips;

    [Header("UI References")]
    [Tooltip("RawImage used to display video output.")]
    public RawImage displayImage;

    [Tooltip("Root panel that covers the screen during intro.")]
    public GameObject introPanel;

    [Tooltip("Root GameObject of the main menu shown after intro.")]
    public GameObject mainMenuPanel;

    [Tooltip("Optional panel to show immediately after the intro (e.g. quiz). " +
             "If set, mainMenuPanel is bypassed until that panel activates it.")]
    public GameObject postIntroPanel;

    [Header("Background Objects")]
    [Tooltip("Background visuals — kept inactive until the main menu is shown.")]
    public GameObject bgObject;

    [Tooltip("Background music — kept inactive until the main menu is shown.")]
    public GameObject bgMusic;

    [Tooltip("Root image that dims the background during skip hint display.")]
    public GameObject skipHintRoot;

    [Tooltip("Base (dim) text label — always fully visible.")]
    public Text skipHintText;

    [Tooltip("RectTransform of the RectMask2D container that reveals the bright text fill.")]
    public RectTransform fillMaskRect;

    [Header("Skip Settings")]
    [Tooltip("Key to hold for skipping the intro.")]
    public KeyCode skipKey = KeyCode.E;

    [Tooltip("How long the player must hold the skip key to trigger skip.")]
    public float skipHoldDuration = 5f;

    [Header("Fade Settings")]
    public float fadeDuration = 0.6f;

    // Internal state
    private VideoPlayer videoPlayer;
    private RenderTexture renderTexture;
    private float skipHeldTime = 0f;
    private bool isSkipping = false;
    private bool introFinished = false;
    private bool hasFadedOut = false;
    private bool skipIntroAndShowMenu = false;  // true когда игрок вернулся из игровой сцены
    private CanvasGroup introPanelCanvasGroup;
    private float fillMaskFullWidth;

    private const string SkipHintMessage      = "Зажмите E чтобы пропустить";
    private const string QuizPassedKey        = "QuizPassed";
    private const string ReturnedFromGameKey  = "ReturnedFromGame";

    private void Awake()
    {
        // Принудительно скрываем ДО любого Start() — независимо от состояния сцены и PlayerPrefs
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (bgObject      != null) bgObject.SetActive(false);
        if (bgMusic       != null) bgMusic.SetActive(false);

        // Если игрок вернулся из игровой сцены — пропускаем интро и квиз
        if (PlayerPrefs.GetInt(ReturnedFromGameKey, 0) == 1)
        {
            PlayerPrefs.DeleteKey(ReturnedFromGameKey);
            PlayerPrefs.Save();
            skipIntroAndShowMenu = true;
            return; // Видеоплеер не нужен — интро не будет играть
        }

        // Обычная инициализация
        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
            videoPlayer = gameObject.AddComponent<VideoPlayer>();

        SetupVideoPlayer();
        SetupRenderTexture();

        introPanelCanvasGroup = introPanel.GetComponent<CanvasGroup>();
        if (introPanelCanvasGroup == null)
            introPanelCanvasGroup = introPanel.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        // Игрок вернулся из игровой сцены — сразу показываем меню
        if (skipIntroAndShowMenu)
        {
            if (introPanel != null) introPanel.SetActive(false);
            ShowMainMenu();
            return;
        }

        // Hide menu, show intro
        mainMenuPanel.SetActive(false);
        introPanel.SetActive(true);
        introPanelCanvasGroup.alpha = 1f;

        if (skipHintRoot != null) skipHintRoot.SetActive(false);

        // Store the full width of the mask parent so we can lerp towards it
        if (fillMaskRect != null)
        {
            // Full width = parent's sizeDelta.x (SkipHintRoot)
            RectTransform parentRect = fillMaskRect.parent as RectTransform;
            fillMaskFullWidth = parentRect != null ? parentRect.rect.width : 420f;
            SetFillProgress(0f);
        }

        StartCoroutine(PlayIntroSequence());
    }

    private void Update()
    {
        if (skipIntroAndShowMenu || introFinished || isSkipping) return;

        if (Input.GetKey(skipKey))
        {
            skipHeldTime += Time.deltaTime;

            if (skipHintRoot != null && !skipHintRoot.activeSelf)
                skipHintRoot.SetActive(true);

            float progress = Mathf.Clamp01(skipHeldTime / skipHoldDuration);
            SetFillProgress(progress);

            if (skipHeldTime >= skipHoldDuration)
                TriggerSkip();
        }
        else
        {
            // Release key — smoothly drain the fill
            if (skipHeldTime > 0f)
            {
                skipHeldTime -= Time.deltaTime * 2f;
                skipHeldTime = Mathf.Max(0f, skipHeldTime);

                SetFillProgress(Mathf.Clamp01(skipHeldTime / skipHoldDuration));

                if (skipHeldTime <= 0f && skipHintRoot != null)
                    skipHintRoot.SetActive(false);
            }
        }
    }

    /// <summary>Advances the text-fill mask to match normalised progress [0..1].</summary>
    private void SetFillProgress(float progress)
    {
        if (fillMaskRect == null) return;
        Vector2 sd = fillMaskRect.sizeDelta;
        sd.x = fillMaskFullWidth * progress;
        fillMaskRect.sizeDelta = sd;
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private void SetupVideoPlayer()
    {
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode  = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.skipOnDrop  = true;
        videoPlayer.isLooping   = false;
    }

    private void SetupRenderTexture()
    {
        renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
        renderTexture.Create();
        videoPlayer.targetTexture = renderTexture;
        if (displayImage != null)
            displayImage.texture = renderTexture;
    }

    private IEnumerator PlayIntroSequence()
    {
        if (introClips == null || introClips.Length == 0)
        {
            FinishIntro();
            yield break;
        }

        // Show skip hint after a short delay
        yield return new WaitForSeconds(1f);
        if (skipHintRoot != null)
        {
            skipHintRoot.SetActive(true);
            if (skipHintText != null) skipHintText.text = SkipHintMessage;
        }

        foreach (VideoClip clip in introClips)
        {
            if (clip == null) continue;

            videoPlayer.clip = clip;
            videoPlayer.Prepare();

            // Wait until prepared
            while (!videoPlayer.isPrepared)
            {
                if (isSkipping) yield break;
                yield return null;
            }

            if (isSkipping) yield break;
            videoPlayer.Play();

            // Wait until clip finishes or skip is triggered
            while (videoPlayer.isPlaying)
            {
                if (isSkipping) yield break;
                yield return null;
            }

            if (isSkipping) yield break;

            // Brief pause between clips
            yield return new WaitForSeconds(0.2f);

            if (isSkipping) yield break;
        }

        FinishIntro();
    }

    private void TriggerSkip()
    {
        if (hasFadedOut) return;
        isSkipping = true;
        videoPlayer.Stop();
        StartCoroutine(FadeOutAndShowMenu());
    }

    private void FinishIntro()
    {
        if (hasFadedOut) return;
        introFinished = true;
        StartCoroutine(FadeOutAndShowMenu());
    }

    private IEnumerator FadeOutAndShowMenu()
    {
        if (hasFadedOut) yield break;
        hasFadedOut = true;
        if (skipHintRoot != null) skipHintRoot.SetActive(false);

        // Fade intro panel to black then hide
        float elapsed = 0f;
        float startAlpha = introPanelCanvasGroup.alpha;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            introPanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
            yield return null;
        }

        introPanelCanvasGroup.alpha = 0f;
        introPanel.SetActive(false);
        videoPlayer.Stop();
        renderTexture.Release();

        // Всегда показываем квиз-панель после интро если она назначена
        if (postIntroPanel != null)
            postIntroPanel.SetActive(true);
        else
            ShowMainMenu();
    }

    /// <summary>Activates the main menu together with background objects.</summary>
    public void ShowMainMenu()
    {
        if (bgObject      != null) bgObject.SetActive(true);
        if (bgMusic       != null) bgMusic.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }
}
