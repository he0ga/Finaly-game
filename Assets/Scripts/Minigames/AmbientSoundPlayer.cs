using UnityEngine;

/// <summary>
/// Plays a looping ambient sound that starts when the game begins and stops when it ends.
/// Add this component (along with an AudioSource) to any GameObject in the hierarchy.
/// Assign your AudioClip to the AudioSource and configure volume/pitch there.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AmbientSoundPlayer : MonoBehaviour
{
    [Tooltip("Seconds to fade in when the game starts. Set to 0 for instant.")]
    public float fadeInDuration = 1f;
    [Tooltip("Seconds to fade out when the game ends. Set to 0 for instant.")]
    public float fadeOutDuration = 1f;

    private AudioSource audioSource;
    private float targetVolume;
    private float currentFadeSpeed;
    private bool isFadingOut;

    // Volume set in the Inspector — used as the "full volume" target.
    private float configuredVolume;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        // Save the volume the user set in the Inspector before we touch it.
        configuredVolume = audioSource.volume;

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;
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

    private void HandleGameStarted()
    {
        isFadingOut = false;
        targetVolume = configuredVolume;
        currentFadeSpeed = fadeInDuration > 0f ? configuredVolume / fadeInDuration : float.MaxValue;

        audioSource.volume = fadeInDuration > 0f ? 0f : configuredVolume;
        audioSource.Play();
    }

    private void HandleGameEnded()
    {
        isFadingOut = true;
        targetVolume = 0f;
        currentFadeSpeed = fadeOutDuration > 0f ? 1f / fadeOutDuration : float.MaxValue;

        if (fadeOutDuration <= 0f)
            audioSource.Stop();
    }

    private void Update()
    {
        if (!audioSource.isPlaying && !isFadingOut) return;

        audioSource.volume = Mathf.MoveTowards(
            audioSource.volume,
            targetVolume,
            currentFadeSpeed * Time.deltaTime
        );

        if (isFadingOut && audioSource.volume <= 0f)
            audioSource.Stop();
    }
}
