using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages a sequence of tutorial slides shown before the minigame starts.
/// Populate the Slides array with any GameObjects you want shown as pages.
/// The Forward button on the last slide triggers the game countdown.
/// </summary>
public class TutorialController : MonoBehaviour
{
    [Header("Slides")]
    [Tooltip("Add as many panel GameObjects here as you need. " +
             "Each one is shown as a separate tutorial page.")]
    public GameObject[] slides;

    [Header("Navigation Buttons")]
    public Button backButton;
    public Button forwardButton;

    [Header("Page Indicator (optional)")]
    [Tooltip("A Text element showing e.g. '1 / 3'. Leave empty to skip.")]
    public Text pageIndicatorText;

    [Header("Button Labels (optional)")]
    [Tooltip("Text component on the Forward button. Used to change label on the last slide.")]
    public Text forwardButtonText;
    [Tooltip("Label shown on Forward button for middle slides.")]
    public string nextLabel = "Далее >";
    [Tooltip("Label shown on Forward button on the last slide.")]
    public string startLabel = "Начать!";

    private int currentIndex = 0;

    private void Start()
    {
        // Ensure the cursor is usable while the player navigates the tutorial.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        if (slides == null || slides.Length == 0)
        {
            // No slides configured — start game immediately.
            gameObject.SetActive(false);
            MinigameManager.Instance.StartCountdown();
            return;
        }

        backButton.onClick.AddListener(OnBack);
        forwardButton.onClick.AddListener(OnForward);

        ShowSlide(0);
    }

    /// <summary>Navigates to the previous slide.</summary>
    private void OnBack()
    {
        if (currentIndex > 0)
            ShowSlide(currentIndex - 1);
    }

    /// <summary>Navigates to the next slide, or starts the game on the last one.</summary>
    private void OnForward()
    {
        if (currentIndex < slides.Length - 1)
        {
            ShowSlide(currentIndex + 1);
        }
        else
        {
            // Last slide — hide tutorial and start game.
            gameObject.SetActive(false);
            MinigameManager.Instance.StartCountdown();
        }
    }

    /// <summary>Shows the slide at the given index and updates navigation state.</summary>
    private void ShowSlide(int index)
    {
        // Hide all slides.
        foreach (GameObject slide in slides)
            if (slide != null) slide.SetActive(false);

        currentIndex = index;

        // Show the current slide.
        if (slides[currentIndex] != null)
            slides[currentIndex].SetActive(true);

        // Back button: hidden on the first slide.
        if (backButton != null)
            backButton.gameObject.SetActive(currentIndex > 0);

        // Forward button label: last slide shows start label.
        if (forwardButtonText != null)
            forwardButtonText.text = (currentIndex == slides.Length - 1) ? startLabel : nextLabel;

        // Page indicator: "2 / 5".
        if (pageIndicatorText != null)
            pageIndicatorText.text = $"{currentIndex + 1} / {slides.Length}";
    }
}
