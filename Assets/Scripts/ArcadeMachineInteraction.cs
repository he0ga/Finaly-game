using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles player interaction with the arcade machine:
/// shows "Press E" hint when looking at it, then plays a suck-in camera animation
/// with white flash and loads the MiniGame scene.
/// </summary>
public class ArcadeMachineInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactDistance = 4f;
    public KeyCode interactKey = KeyCode.E;
    public string miniGameSceneName = "MiniGame_1";

    [Header("References")]
    public Camera playerCamera;
    public WASDMouseMovement playerMovement;
    public AudioSource backgroundMusic;

    [Header("UI")]
    public Text pressEText;
    public Image whiteFlashImage;

    [Header("Suck-in Animation")]
    [Tooltip("Optional target point the camera flies into. " +
             "Assign an empty GameObject placed at the arcade screen slot. " +
             "If left empty, the camera moves toward the machine's pivot.")]
    public Transform suckInPoint;

    [Tooltip("How fast the music fades out in seconds.")]
    public float musicFadeDuration = 1.5f;
    [Tooltip("Total duration of the camera suck-in animation in seconds.")]
    public float suckInDuration = 2.5f;
    [Tooltip("Used only when Suck In Point is not assigned: " +
             "how far from the machine pivot the camera stops.")]
    public float suckInTargetDistance = 0.3f;
    [Tooltip("Normalized time (0–1) within the suck-in animation at which the white overlay starts appearing. " +
             "0 = fades from the very beginning, 0.5 = starts at the halfway point, 1 = only at the end.")]
    [Range(0f, 1f)]
    public float flashStartThreshold = 0.4f;
    [Tooltip("Duration of the final full-white hold before the scene loads, in seconds.")]
    public float flashDuration = 0.5f;

    [Header("Sound Effect (assign later)")]
    public AudioSource suckInSoundSource;

    private bool isAnimating = false;

    private void Start()
    {
        if (pressEText != null)
            pressEText.gameObject.SetActive(false);

        if (whiteFlashImage != null)
        {
            whiteFlashImage.color = new Color(1f, 1f, 1f, 0f);
            whiteFlashImage.gameObject.SetActive(false);
        }

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerMovement == null)
            playerMovement = FindObjectOfType<WASDMouseMovement>();

        if (backgroundMusic == null)
        {
            GameObject bgObj = GameObject.Find("BG_Music");
            if (bgObj != null)
                backgroundMusic = bgObj.GetComponent<AudioSource>();
        }
    }

    private void Update()
    {
        if (isAnimating) return;

        bool lookingAtMachine = IsLookingAtMachine(out float distance);

        if (pressEText != null)
            pressEText.gameObject.SetActive(lookingAtMachine && distance <= interactDistance);

        if (lookingAtMachine && distance <= interactDistance && Input.GetKeyDown(interactKey))
            StartCoroutine(PlaySuckInSequence());
    }

    /// <summary>Casts a ray from the center of the screen and checks if it hits this machine.</summary>
    private bool IsLookingAtMachine(out float distance)
    {
        distance = float.MaxValue;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                distance = hit.distance;
                return true;
            }
        }

        return false;
    }

    /// <summary>Full interaction sequence: fade music, play suck-in, white flash, load scene.</summary>
    private IEnumerator PlaySuckInSequence()
    {
        isAnimating = true;

        if (pressEText != null)
            pressEText.gameObject.SetActive(false);

        // Disable player movement and look
        if (playerMovement != null)
            playerMovement.enabled = false;

        // Play suck-in sound effect if assigned
        if (suckInSoundSource != null && suckInSoundSource.clip != null)
            suckInSoundSource.Play();

        // Fade out background music, move camera, and fade in white overlay simultaneously
        yield return StartCoroutine(FadeMusicAndSuckIn());

        // Hold full white briefly before loading
        yield return StartCoroutine(PlayWhiteFlash());

        SceneManager.LoadScene(miniGameSceneName);
    }

    /// <summary>Resolves the world-space target position and rotation for the suck-in animation.</summary>
    private (Vector3 position, Quaternion rotation) ResolveSuckInTarget()
    {
        if (suckInPoint != null)
        {
            // Use the assigned Transform: camera flies to its position and adopts its rotation.
            return (suckInPoint.position, suckInPoint.rotation);
        }

        // Fallback: stop just in front of the machine pivot.
        Vector3 dir = (transform.position - playerCamera.transform.position).normalized;
        Vector3 pos = transform.position - dir * suckInTargetDistance;
        Quaternion rot = Quaternion.LookRotation(dir);
        return (pos, rot);
    }

    /// <summary>Fades out the background music while animating the camera toward the machine.
    /// The white overlay starts fading in once the animation reaches flashStartThreshold.</summary>
    private IEnumerator FadeMusicAndSuckIn()
    {
        float elapsed = 0f;
        float startMusicVolume = backgroundMusic != null ? backgroundMusic.volume : 0f;

        Vector3 cameraStartPos = playerCamera.transform.position;
        Quaternion cameraStartRot = playerCamera.transform.rotation;

        (Vector3 targetPos, Quaternion targetRot) = ResolveSuckInTarget();

        // Activate overlay at zero alpha before the loop
        if (whiteFlashImage != null)
        {
            whiteFlashImage.color = new Color(1f, 1f, 1f, 0f);
            whiteFlashImage.gameObject.SetActive(true);
        }

        // Detach camera from player so it can move freely
        playerCamera.transform.SetParent(null);

        while (elapsed < suckInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / suckInDuration;
            float eased = Mathf.SmoothStep(0f, 1f, t);

            // Accelerate toward the end (suck-in feel)
            float accel = Mathf.Pow(t, 2f);

            playerCamera.transform.position = Vector3.Lerp(cameraStartPos, targetPos, accel);
            playerCamera.transform.rotation = Quaternion.Slerp(cameraStartRot, targetRot, eased);

            // Narrow FOV slightly for zoom effect
            playerCamera.fieldOfView = Mathf.Lerp(60f, 40f, eased);

            // Fade music
            if (backgroundMusic != null)
                backgroundMusic.volume = Mathf.Lerp(startMusicVolume, 0f, t);

            // White overlay: ramp from 0 to 1 between flashStartThreshold and animation end
            if (whiteFlashImage != null)
            {
                float flashRange = 1f - flashStartThreshold;
                float flashT = flashRange > 0f
                    ? Mathf.Clamp01((t - flashStartThreshold) / flashRange)
                    : (t >= flashStartThreshold ? 1f : 0f);
                whiteFlashImage.color = new Color(1f, 1f, 1f, flashT);
            }

            yield return null;
        }

        if (backgroundMusic != null)
        {
            backgroundMusic.volume = 0f;
            backgroundMusic.Stop();
        }

        if (whiteFlashImage != null)
            whiteFlashImage.color = new Color(1f, 1f, 1f, 1f);
    }

    /// <summary>Holds the fully white screen for flashDuration seconds before the scene loads.</summary>
    private IEnumerator PlayWhiteFlash()
    {
        if (whiteFlashImage == null) yield break;

        yield return new WaitForSeconds(flashDuration);
    }

    private void OnDrawGizmosSelected()
    {
        if (suckInPoint != null)
        {
            // Draw the custom suck-in target: sphere + forward arrow + connection line.
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(suckInPoint.position, 0.1f);
            Gizmos.DrawLine(suckInPoint.position, suckInPoint.position + suckInPoint.forward * 0.4f);
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.DrawLine(transform.position, suckInPoint.position);
        }
        else
        {
            // Draw the fallback target position.
            Gizmos.color = Color.yellow;
            Vector3 dir = suckInTargetDistance > 0f ? -transform.forward : Vector3.back;
            Vector3 fallbackPos = transform.position - dir * suckInTargetDistance;
            Gizmos.DrawWireSphere(fallbackPos, 0.1f);
        }

        // Always draw the interaction radius.
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}
