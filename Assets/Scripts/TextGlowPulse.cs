using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pulses the alpha of a UI Outline component to create a soft white glowing
/// outline effect. The Text fill color is left unchanged.
/// Requires an Outline component on the same GameObject.
/// </summary>
[RequireComponent(typeof(Outline))]
public class TextGlowPulse : MonoBehaviour
{
    [Tooltip("Color of the outline at maximum glow. Keep near-white for a glow look.")]
    public Color outlineGlowColor = Color.white;

    [Tooltip("Minimum alpha of the outline (0–1). Lower = more dramatic pulse.")]
    [Range(0f, 1f)]
    public float minAlpha = 0.01f;

    [Tooltip("Maximum alpha of the outline (0–1).")]
    [Range(0f, 1f)]
    public float maxAlpha = 0.2f;

    [Tooltip("Pulse cycles per second.")]
    public float pulseSpeed = 0.6f;

    private Outline outline;

    private void Awake()
    {
        outline = GetComponent<Outline>();
    }

    private void Update()
    {
        // Smooth sine wave mapped to [0, 1]
        float t = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;

        Color color = outlineGlowColor;
        color.a = Mathf.Lerp(minAlpha, maxAlpha, t);
        outline.effectColor = color;
    }
}
