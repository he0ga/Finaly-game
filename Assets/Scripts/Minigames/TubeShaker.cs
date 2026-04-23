using System.Collections;
using UnityEngine;

/// <summary>
/// Randomly shakes the Tube and BackgroundScroller by the same positional offset,
/// creating the illusion that the whole tunnel trembles.
/// Only active while the minigame is running (listens to MinigameManager events).
/// </summary>
public class TubeShaker : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("Root transform of the Tube (walls and finish move with it).")]
    public Transform tube;

    [Tooltip("Root transform of the BackgroundScroller (all tiles are children and move with it).")]
    public Transform backgroundScroller;

    [Header("Timing")]
    [Tooltip("Minimum seconds to wait between shake events.")]
    public float minInterval = 3f;

    [Tooltip("Maximum seconds to wait between shake events.")]
    public float maxInterval = 8f;

    [Header("Shake Shape")]
    [Tooltip("Duration of a single shake event in seconds.")]
    public float shakeDuration = 0.45f;

    [Tooltip("Maximum positional offset in world units.")]
    public float shakeIntensity = 0.15f;

    [Tooltip("Oscillation speed — higher values produce faster, choppier shaking.")]
    public float shakeSpeed = 30f;

    // ── Private state ──────────────────────────────────────────────────────
    private Vector3 tubeOrigin;
    private Vector3 bgOrigin;
    private bool isGameActive = false;
    private Coroutine shakeLoopCoroutine;

    // ── Unity lifecycle ────────────────────────────────────────────────────

    private void Awake()
    {
        tubeOrigin = tube != null ? tube.localPosition : Vector3.zero;
        bgOrigin   = backgroundScroller != null ? backgroundScroller.localPosition : Vector3.zero;
    }

    private void OnEnable()
    {
        MinigameManager.OnGameStarted += HandleGameStarted;
        MinigameManager.OnGameEnded   += HandleGameEnded;
    }

    private void OnDisable()
    {
        MinigameManager.OnGameStarted -= HandleGameStarted;
        MinigameManager.OnGameEnded   -= HandleGameEnded;
    }

    // ── MinigameManager event handlers ─────────────────────────────────────

    private void HandleGameStarted()
    {
        isGameActive = true;
        shakeLoopCoroutine = StartCoroutine(ShakeLoop());
    }

    private void HandleGameEnded()
    {
        isGameActive = false;

        if (shakeLoopCoroutine != null)
        {
            StopCoroutine(shakeLoopCoroutine);
            shakeLoopCoroutine = null;
        }

        RestorePositions();
    }

    // ── Coroutines ─────────────────────────────────────────────────────────

    private IEnumerator ShakeLoop()
    {
        while (isGameActive)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));

            if (isGameActive)
                yield return PerformShake();
        }
    }

    private IEnumerator PerformShake()
    {
        float elapsed = 0f;
        float seed    = Random.value * 100f;

        while (elapsed < shakeDuration)
        {
            float t        = elapsed / shakeDuration;
            float envelope = Mathf.Sin(t * Mathf.PI); // bell-curve: 0 → peak → 0

            float x = (Mathf.PerlinNoise(seed + elapsed * shakeSpeed * 0.1f, 0f) * 2f - 1f)
                      * shakeIntensity * envelope;
            float y = (Mathf.PerlinNoise(0f, seed + elapsed * shakeSpeed * 0.1f) * 2f - 1f)
                      * shakeIntensity * envelope;

            ApplyOffset(new Vector3(x, y, 0f));

            elapsed += Time.deltaTime;
            yield return null;
        }

        RestorePositions();
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private void ApplyOffset(Vector3 offset)
    {
        if (tube != null)
            tube.localPosition = tubeOrigin + offset;

        if (backgroundScroller != null)
            backgroundScroller.localPosition = bgOrigin + offset;
    }

    private void RestorePositions()
    {
        if (tube != null)
            tube.localPosition = tubeOrigin;

        if (backgroundScroller != null)
            backgroundScroller.localPosition = bgOrigin;
    }
}
