using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Typewriter animation for a TMP_Text component.
/// Characters appear one by one. Words written entirely in CAPS also shake.
/// A first capital letter of a regular word (e.g. "Hello") is NOT treated as caps.
/// Attach directly to any GameObject that has a TMP_Text component.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class TypewriterEffect : MonoBehaviour
{
    [Header("Typing")]
    [Tooltip("How many characters are revealed per second.")]
    public float charactersPerSecond = 20f;
    [Tooltip("Speed multiplier applied to fully-caps words. " +
             "0.5 = twice as slow as regular characters.")]
    [Range(0.1f, 1f)]
    public float capsSpeedMultiplier = 0.5f;

    [Header("Caps Shake")]
    [Tooltip("Shake radius in pixels. Applies only to fully-caps words.")]
    public float shakeIntensity = 2.5f;
    [Tooltip("How fast the shake oscillates.")]
    [Range(5f, 60f)]
    public float shakeSpeed = 30f;

    [Header("Caps Color")]
    [Tooltip("Color applied to every character that belongs to a fully-caps word.")]
    public Color capsWordColor = new Color(1f, 0.25f, 0.25f, 1f);

    [Header("Audio")]
    [Tooltip("Played when a regular character is typed.")]
    public AudioSource normalTypeSound;
    [Tooltip("Played when a character from a fully-caps word is typed.")]
    public AudioSource capsTypeSound;

    private TMP_Text tmp;

    // Per-character flag: true if this character belongs to an all-caps word.
    private bool[] isInCapsWord;
    private int visibleCount;
    private Coroutine typingCoroutine;

    private void Awake()
    {
        tmp = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        Play();
    }

    /// <summary>Restarts the typewriter animation from the beginning.</summary>
    public void Play()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        visibleCount = 0;
        tmp.maxVisibleCharacters = 0;
        typingCoroutine = StartCoroutine(TypeCoroutine());
    }

    private IEnumerator TypeCoroutine()
    {
        // Rebuild mesh so textInfo is fully populated before we start reading it.
        tmp.ForceMeshUpdate();

        int total = tmp.textInfo.characterCount;

        // Analyse which characters belong to fully-caps words.
        isInCapsWord = BuildCapsWordMap(tmp.textInfo, total);

        float baseDelay = 1f / Mathf.Max(1f, charactersPerSecond);

        for (int i = 0; i < total; i++)
        {
            visibleCount = i + 1;
            tmp.maxVisibleCharacters = visibleCount;

            bool isCaps = isInCapsWord[i];

            // Caps words type slower.
            float delay = isCaps ? baseDelay / capsSpeedMultiplier : baseDelay;

            if (isCaps)
            {
                if (capsTypeSound != null) capsTypeSound.Play();
            }
            else
            {
                if (normalTypeSound != null) normalTypeSound.Play();
            }

            yield return new WaitForSeconds(delay);
        }

        typingCoroutine = null;
    }

    // LateUpdate runs after Unity's own animations — safe to modify mesh vertices here.
    private void LateUpdate()
    {
        if (visibleCount == 0 || isInCapsWord == null) return;
        ApplyCapsShake();
    }

    private void ApplyCapsShake()
    {
        // ForceMeshUpdate resets vertex positions and colors to their rest state,
        // then we apply shake and color on top.
        tmp.ForceMeshUpdate();

        TMP_TextInfo info = tmp.textInfo;
        bool anyModified = false;
        Color32 capsColor32 = capsWordColor;

        for (int i = 0; i < visibleCount && i < info.characterCount; i++)
        {
            if (!isInCapsWord[i]) continue;

            TMP_CharacterInfo ci = info.characterInfo[i];
            if (!ci.isVisible) continue; // skip spaces, line breaks, etc.

            int meshIdx = ci.materialReferenceIndex;
            int vertIdx = ci.vertexIndex;

            // ── Shake ─────────────────────────────────────────────────────────
            Vector3[] verts = info.meshInfo[meshIdx].vertices;
            float t = Time.time * shakeSpeed;
            Vector3 shake = new Vector3(
                Mathf.Sin(t + i * 1.73f) * shakeIntensity,
                Mathf.Cos(t * 1.3f + i * 2.11f) * shakeIntensity,
                0f
            );
            verts[vertIdx + 0] += shake;
            verts[vertIdx + 1] += shake;
            verts[vertIdx + 2] += shake;
            verts[vertIdx + 3] += shake;

            // ── Color ─────────────────────────────────────────────────────────
            Color32[] colors = info.meshInfo[meshIdx].colors32;
            colors[vertIdx + 0] = capsColor32;
            colors[vertIdx + 1] = capsColor32;
            colors[vertIdx + 2] = capsColor32;
            colors[vertIdx + 3] = capsColor32;

            anyModified = true;
        }

        if (!anyModified) return;

        for (int m = 0; m < info.meshInfo.Length; m++)
        {
            info.meshInfo[m].mesh.vertices = info.meshInfo[m].vertices;
            tmp.UpdateGeometry(info.meshInfo[m].mesh, m);
        }

        // Upload modified colors separately.
        tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    /// <summary>
    /// Scans textInfo and returns a bool array where [i] is true only if
    /// character i belongs to a word whose every letter is uppercase.
    /// Single capital letters at the start of a word (title-case) are NOT flagged.
    /// </summary>
    private static bool[] BuildCapsWordMap(TMP_TextInfo info, int count)
    {
        bool[] result = new bool[count];

        int i = 0;
        while (i < count)
        {
            char c = info.characterInfo[i].character;

            if (char.IsLetter(c))
            {
                // Collect the full word.
                int wordStart = i;
                while (i < count && char.IsLetter(info.characterInfo[i].character))
                    i++;
                int wordEnd = i; // exclusive

                // Qualifies as ALL-CAPS only when every letter is uppercase.
                // Single-letter words (e.g. "I", "А") are excluded to avoid odd lone shaking.
                bool allCaps = (wordEnd - wordStart) > 1;
                for (int j = wordStart; j < wordEnd && allCaps; j++)
                {
                    if (!char.IsUpper(info.characterInfo[j].character))
                        allCaps = false;
                }

                if (allCaps)
                {
                    for (int j = wordStart; j < wordEnd; j++)
                        result[j] = true;
                }
            }
            else
            {
                i++;
            }
        }

        return result;
    }
}
