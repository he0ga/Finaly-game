using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Central state manager for MiniGame_1.
/// Handles start countdown, game over on wall collision, and victory on finish.
/// </summary>
public class MinigameManager : MonoBehaviour
{
    public static MinigameManager Instance { get; private set; }

    [Header("References")]
    public LampaController lampa;

    [Header("UI")]
    public GameObject startPanel;
    public GameObject gameOverPanel;
    public GameObject winPanel;
    public Text countdownText;

    [Header("Settings")]
    [Tooltip("Scene to load when player wins (leave empty to stay on this scene).")]
    public string nextSceneName = "";
    [Tooltip("Delay in seconds before restarting after Game Over.")]
    public float restartDelay = 2f;
    [Tooltip("Delay in seconds before loading next scene after win.")]
    public float winDelay = 2f;

    private bool gameStarted = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        lampa.SetActive(false);
        SetPanels(startPanel: true, gameOver: false, win: false);
        StartCoroutine(StartCountdown());
    }

    /// <summary>Called by wall colliders when the lamp touches a wall.</summary>
    public void OnWallHit()
    {
        if (!gameStarted) return;
        gameStarted = false;
        lampa.SetActive(false);
        SetPanels(startPanel: false, gameOver: true, win: false);
        StartCoroutine(RestartAfterDelay());
    }

    /// <summary>Called by the finish trigger when the lamp reaches the bottom.</summary>
    public void OnFinishReached()
    {
        if (!gameStarted) return;
        gameStarted = false;
        lampa.SetActive(false);
        SetPanels(startPanel: false, gameOver: false, win: true);

        if (!string.IsNullOrEmpty(nextSceneName))
            StartCoroutine(LoadNextSceneAfterDelay());
    }

    private IEnumerator StartCountdown()
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);

            string[] steps = { "3", "2", "1", "GO!" };
            foreach (string step in steps)
            {
                countdownText.text = step;
                yield return new WaitForSeconds(1f);
            }

            countdownText.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }

        SetPanels(startPanel: false, gameOver: false, win: false);
        gameStarted = true;
        lampa.SetActive(true);
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(restartDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private IEnumerator LoadNextSceneAfterDelay()
    {
        yield return new WaitForSeconds(winDelay);
        SceneManager.LoadScene(nextSceneName);
    }

    private void SetPanels(bool startPanel, bool gameOver, bool win)
    {
        if (this.startPanel != null) this.startPanel.SetActive(startPanel);
        if (gameOverPanel != null) gameOverPanel.SetActive(gameOver);
        if (winPanel != null) winPanel.SetActive(win);
    }
}
