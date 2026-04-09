using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Smooth hover animation for quiz answer buttons.
/// Scales the button up and applies a tint when the pointer enters,
/// and reverses the effect on exit.
/// Attach to a GameObject that also has an <see cref="Image"/> and a <see cref="Button"/>.
/// </summary>
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(Button))]
public class AnswerButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Scale multiplier applied on hover (e.g. 1.06 = 6% larger).")]
    public float hoverScale = 1.06f;

    [Tooltip("Tint color blended into the background on hover.")]
    public Color hoverTint = new Color(1f, 0.85f, 0.45f, 0.2f);

    [Tooltip("Duration of the scale and tint transition in seconds.")]
    public float animDuration = 0.1f;

    // ── Private state ──────────────────────────────────────────────────────
    private Image backgroundImage;
    private Button button;
    private Coroutine animCoroutine;
    private Color idleColor;

    private void Awake()
    {
        backgroundImage = GetComponent<Image>();
        button          = GetComponent<Button>();
        idleColor       = backgroundImage.color;
    }

    // ── IPointerEnterHandler / IPointerExitHandler ─────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;
        PlayAnimation(hoverScale, hoverTint);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;
        PlayAnimation(1f, idleColor);
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Instantly resets scale and color to idle values.
    /// Call this before disabling <see cref="Button.interactable"/> to avoid stuck hover state.
    /// </summary>
    public void ResetToIdle()
    {
        StopAnimation();
        transform.localScale           = Vector3.one;
        if (backgroundImage != null)
            backgroundImage.color = idleColor;
    }

    /// <summary>
    /// Updates the resting color reference used when the pointer exits.
    /// Call this when <see cref="QuizManager"/> sets feedback colors on the button.
    /// </summary>
    public void SetIdleColor(Color color)
    {
        idleColor = color;
        if (backgroundImage != null)
            backgroundImage.color = color;
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private void PlayAnimation(float targetScale, Color targetColor)
    {
        StopAnimation();
        animCoroutine = StartCoroutine(AnimationCoroutine(targetScale, targetColor));
    }

    private void StopAnimation()
    {
        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
            animCoroutine = null;
        }
    }

    private IEnumerator AnimationCoroutine(float targetScale, Color targetColor)
    {
        Vector3 startScale = transform.localScale;
        Color   startColor = backgroundImage != null ? backgroundImage.color : Color.clear;
        Vector3 endScale   = new Vector3(targetScale, targetScale, 1f);

        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / animDuration));
            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            if (backgroundImage != null)
                backgroundImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        transform.localScale = endScale;
        if (backgroundImage != null)
            backgroundImage.color = targetColor;

        animCoroutine = null;
    }
}
