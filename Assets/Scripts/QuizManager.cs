using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// Presents a sequence of questions from a <see cref="QuizData"/> asset.
/// Answer buttons are created dynamically — any number of answers per question is supported.
/// Call <see cref="StartQuiz"/> to begin (wired from <see cref="IntroPlayer"/> via postIntroPanel activation).
/// </summary>
public class QuizManager : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("ScriptableObject with all quiz questions. Create via Assets → Create → Game → Quiz Data.")]
    public QuizData quizData;

    [Header("Panels")]
    [Tooltip("Root panel of the quiz UI.")]
    public GameObject quizPanel;

    [Tooltip("Main menu panel to activate when the quiz is finished.")]
    public GameObject mainMenuPanel;

    [Tooltip("Background video GameObject (activated together with the main menu).")]
    public GameObject bgObject;

    [Tooltip("Background music GameObject (activated together with the main menu).")]
    public GameObject bgMusic;

    [Header("UI Elements")]
    [Tooltip("Text element that displays the question.")]
    public Text questionText;

    [Tooltip("Parent transform where answer buttons are spawned (VerticalLayoutGroup is added at runtime).")]
    public RectTransform answerContainer;

    [Tooltip("Optional text shown briefly after each answer (correct / wrong feedback).")]
    public Text feedbackText;

    [Header("Answer Button Appearance")]
    [Tooltip("Resting background color of each button. Set alpha to 0 for fully transparent.")]
    public Color defaultButtonColor   = new Color(0f, 0f, 0f, 0f);

    [Tooltip("Background color shown when the player picks the correct answer.")]
    public Color correctButtonColor   = new Color(0.18f, 0.65f, 0.25f, 0.2f);

    [Tooltip("Background color shown when the player picks an incorrect answer.")]
    public Color incorrectButtonColor = new Color(0.75f, 0.18f, 0.18f, 0f);

    [Tooltip("Height of each answer button in pixels.")]
    public float buttonHeight = 64f;

    [Tooltip("Font size for answer button labels. Adjust freely — transparent background avoids clipping.")]
    public int answerFontSize = 30;

    [Header("Behaviour")]
    [Tooltip("Seconds to display correct/incorrect feedback before advancing or quitting.")]
    public float feedbackDuration = 1.5f;

    [Header("Typewriter Effect")]
    [Tooltip("Seconds between each revealed character.")]
    public float charDelay = 0.04f;

    [Tooltip("Sound played on each character reveal.")]
    public AudioClip typingSound;

    [Tooltip("Volume of the per-character typing sound.")]
    [Range(0f, 1f)]
    public float typingVolume = 0.4f;

    [Header("Quiz Background Audio")]
    [Tooltip("Looping ambient sound played while the quiz is active (e.g. wind_sound.mp3).")]
    public AudioClip quizBgClip;

    [Tooltip("Volume of the quiz background ambient sound.")]
    [Range(0f, 1f)]
    public float quizBgVolume = 0.5f;

    [Tooltip("Volume of the quiz background ambient sound.")]
    [Range(0f, 1f)]
    public AudioMixerGroup mixerGroupbg;

    [Tooltip("Volume of the quiz background ambient sound.")]
    [Range(0f, 1f)]
    public AudioMixerGroup mixerGrouptyping;

    // ── Private state ──────────────────────────────────────────────────────
    private int currentIndex = 0;
    private bool isWaitingForNext = false;
    private readonly List<Button> spawnedButtons = new List<Button>();
    private Coroutine typewriterCoroutine;
    private AudioSource typingAudioSource;
    private AudioSource bgAudioSource;

    private const string QuizPassedKey        = "QuizPassed";
    private const string QuizSavedQuestionKey = "QuizCurrentQuestion";

    // ── Unity lifecycle ────────────────────────────────────────────────────

    private void Awake()
    {
        SetupAnswerContainer();
        SetupAudioSource();
    }

    private void OnEnable()
    {
        // Called when IntroPlayer activates quizPanel
        StartQuiz();
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>Starts the quiz from the last unanswered question (or from the beginning).</summary>
    public void StartQuiz()
    {
        if (quizData == null || quizData.questions == null || quizData.questions.Length == 0)
        {
            Debug.LogWarning("[QuizManager] No QuizData assigned or it has no questions. Skipping quiz.");
            FinishQuiz();
            return;
        }

        // Восстанавливаем индекс вопроса на котором игрок ошибся в прошлый раз
        currentIndex     = PlayerPrefs.GetInt(QuizSavedQuestionKey, 0);
        currentIndex     = Mathf.Clamp(currentIndex, 0, quizData.questions.Length - 1);
        isWaitingForNext = false;

        if (feedbackText != null)
        {
            feedbackText.text  = string.Empty;
            feedbackText.color = Color.white;
        }

        PlayQuizBg();
        ShowCurrentQuestion();
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private void SetupAudioSource()
    {
        // Первый AudioSource — для звука печати (PlayOneShot)
        typingAudioSource = GetComponent<AudioSource>();
        if (typingAudioSource == null)
            typingAudioSource = gameObject.AddComponent<AudioSource>();

        typingAudioSource.playOnAwake  = false;
        typingAudioSource.spatialBlend = 0f;
        typingAudioSource.outputAudioMixerGroup = mixerGrouptyping;

        // Второй AudioSource — для фонового лупа (ветер)
        bgAudioSource = gameObject.AddComponent<AudioSource>();
        bgAudioSource.playOnAwake  = false;
        bgAudioSource.spatialBlend = 0f;
        bgAudioSource.loop         = true;
        bgAudioSource.volume       = quizBgVolume;
        bgAudioSource.outputAudioMixerGroup = mixerGroupbg;
    }

    private void SetupAnswerContainer()
    {
        if (answerContainer == null) return;

        VerticalLayoutGroup vlg = answerContainer.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
            vlg = answerContainer.gameObject.AddComponent<VerticalLayoutGroup>();

        vlg.spacing              = 10f;
        vlg.childControlWidth    = true;
        vlg.childControlHeight   = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(16, 16, 8, 8);
    }

    private void ShowCurrentQuestion()
    {
        if (currentIndex >= quizData.questions.Length)
        {
            FinishQuiz();
            return;
        }

        QuizQuestion question = quizData.questions[currentIndex];

        // Кнопки появляются сразу, текст — через typewriter
        SpawnAnswerButtons(question);

        if (questionText != null)
        {
            if (typewriterCoroutine != null)
                StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = StartCoroutine(TypewriterEffect(question.questionText));
        }
    }

    /// <summary>Reveals question text character by character with a typing sound.</summary>
    private IEnumerator TypewriterEffect(string fullText)
    {
        questionText.text = string.Empty;

        foreach (char c in fullText)
        {
            questionText.text += c;

            if (typingSound != null && typingAudioSource != null)
                typingAudioSource.PlayOneShot(typingSound, typingVolume);

            yield return new WaitForSeconds(charDelay);
        }

        typewriterCoroutine = null;
    }

    private void PlayQuizBg()
    {
        if (bgAudioSource == null || quizBgClip == null) return;
        bgAudioSource.clip   = quizBgClip;
        bgAudioSource.volume = quizBgVolume;
        bgAudioSource.Play();
    }

    private void StopQuizBg()
    {
        if (bgAudioSource != null && bgAudioSource.isPlaying)
            bgAudioSource.Stop();
    }

    private void SpawnAnswerButtons(QuizQuestion question)
    {
        // Destroy previously spawned buttons
        foreach (Button btn in spawnedButtons)
        {
            if (btn != null) Destroy(btn.gameObject);
        }
        spawnedButtons.Clear();

        if (question.answers == null) return;

        for (int i = 0; i < question.answers.Length; i++)
        {
            int capturedIndex = i; // capture for lambda

            // ── Button root ──────────────────────────────────────────────
            GameObject btnGO = new GameObject($"Answer_{i}");
            btnGO.transform.SetParent(answerContainer, false);

            RectTransform rt = btnGO.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, buttonHeight);

            Image bg = btnGO.AddComponent<Image>();
            bg.color = defaultButtonColor;

            Button btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = bg;

            // Disable Unity's built-in color transition — AnswerButtonHover handles it
            btn.transition = Selectable.Transition.None;

            // Add hover animation component
            AnswerButtonHover hover = btnGO.AddComponent<AnswerButtonHover>();

            btn.onClick.AddListener(() => OnAnswerSelected(capturedIndex));

            // ── Label ────────────────────────────────────────────────────
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(btnGO.transform, false);

            RectTransform labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(12f, 4f);
            labelRT.offsetMax = new Vector2(-12f, -4f);

            Text label = labelGO.AddComponent<Text>();
            label.text          = question.answers[i];
            label.font          = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize      = answerFontSize;
            label.alignment     = TextAnchor.MiddleCenter;
            label.color         = Color.white;
            label.raycastTarget = false;

            spawnedButtons.Add(btn);
        }
    }

    /// <summary>Called when the player clicks an answer button.</summary>
    private void OnAnswerSelected(int selectedIndex)
    {
        if (isWaitingForNext) return;

        bool isCorrect = selectedIndex == quizData.questions[currentIndex].correctAnswerIndex;

        // Highlight selected and correct buttons
        HighlightButtons(selectedIndex, isCorrect);

        StartCoroutine(HandleAnswerFeedback(isCorrect));
    }

    private void HighlightButtons(int selectedIndex, bool isCorrect)
    {
        int correctIndex = quizData.questions[currentIndex].correctAnswerIndex;

        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            Button btn = spawnedButtons[i];

            // Reset hover state before disabling interactable so scale snaps back
            AnswerButtonHover hover = btn.GetComponent<AnswerButtonHover>();
            if (hover != null) hover.ResetToIdle();

            btn.interactable = false;

            Color targetColor;
            if (i == correctIndex)
                targetColor = correctButtonColor;
            else if (i == selectedIndex && !isCorrect)
                targetColor = incorrectButtonColor;
            else
                continue;

            // SetIdleColor updates both the stored idle reference and the Image color
            if (hover != null)
                hover.SetIdleColor(targetColor);
            else
            {
                Image bg = btn.GetComponent<Image>();
                if (bg != null) bg.color = targetColor;
            }
        }
    }

    private IEnumerator HandleAnswerFeedback(bool isCorrect)
    {
        isWaitingForNext = true;

        // Остановить typewriter если ещё идёт
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
            if (questionText != null)
                questionText.text = quizData.questions[currentIndex].questionText;
        }

        if (feedbackText != null)
        {
            feedbackText.color = isCorrect ? correctButtonColor : incorrectButtonColor;
            feedbackText.text  = isCorrect ? "Правильно!" : "Неправильно! Игра закрывается...";
        }

        yield return new WaitForSeconds(feedbackDuration);

        if (!isCorrect)
        {
            // Сохраняем индекс неправильно отвеченного вопроса перед закрытием
            PlayerPrefs.SetInt(QuizSavedQuestionKey, currentIndex);
            PlayerPrefs.Save();

            StopQuizBg();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            yield break;
        }

        if (feedbackText != null)
            feedbackText.text = string.Empty;

        currentIndex++;
        isWaitingForNext = false;
        ShowCurrentQuestion();
    }

    private void FinishQuiz()
    {
        StopQuizBg();

        // Все вопросы пройдены — сохраняем флаг и очищаем сохранённый индекс
        PlayerPrefs.SetInt(QuizPassedKey, 1);
        PlayerPrefs.DeleteKey(QuizSavedQuestionKey);
        PlayerPrefs.Save();

        if (quizPanel != null) quizPanel.SetActive(false);
        ShowMenu();
    }

    /// <summary>Activates the main menu together with its background visuals and music.</summary>
    private void ShowMenu()
    {
        // Делегируем IntroPlayer — единая точка активации меню
        IntroPlayer introPlayer = FindObjectOfType<IntroPlayer>();
        if (introPlayer != null)
        {
            introPlayer.ShowMainMenu();
        }
        else
        {
            // Fallback если IntroPlayer не найден
            if (bgObject      != null) bgObject.SetActive(true);
            if (bgMusic       != null) bgMusic.SetActive(true);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        }
    }
}
