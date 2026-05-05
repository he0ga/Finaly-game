using System.Collections;
using Cinemachine;
using UnityEngine;

/// <summary>
/// Controls the screamer virtual camera.
///
/// Normal state   — CM_DefaultVCam (Priority 5) mirrors Player transform;
///                  Brain is always live but outputs the same values, so NavigationScript works freely.
///
/// Screamer state — CM_ScreamerVCam (Priority 10) goes active.
///                  CM_DefaultVCam is frozen at the snapshot of the moment the screamer starts,
///                  Brain blends IN to CM_ScreamerVCam, NavigationScript is paused.
///
/// Return         — CM_ScreamerVCam deactivates, Brain blends OUT back to the frozen
///                  CM_DefaultVCam (= original position). After blend-out finishes,
///                  CM_DefaultVCam unfreezes and NavigationScript resumes.
/// </summary>
[RequireComponent(typeof(CinemachineBrain))]
public class ScreamerCameraController : MonoBehaviour
{
    [Header("Virtual Cameras")]
    [SerializeField] private CinemachineVirtualCamera screamerVCam;
    [SerializeField] private CinemachineFollowTransform defaultVCamFollower;

    [Header("Blend In (normal → screamer)")]
    [SerializeField] private CinemachineBlendDefinition.Style blendInStyle = CinemachineBlendDefinition.Style.EaseInOut;
    [SerializeField] private float blendInDuration = 0.4f;

    [Header("Blend Out (screamer → normal)")]
    [SerializeField] private CinemachineBlendDefinition.Style blendOutStyle = CinemachineBlendDefinition.Style.EaseInOut;
    [SerializeField] private float blendOutDuration = 0.5f;

    private CinemachineBrain brain;
    private NavigationScript navigationScript;
    private Coroutine autoStopCoroutine;
    private Coroutine resumeCoroutine;

    private void Awake()
    {
        brain = GetComponent<CinemachineBrain>();
        navigationScript = GetComponent<NavigationScript>();
    }

    /// <summary>
    /// Activates the screamer camera.
    /// Pass a positive autoDuration to stop automatically after that many seconds.
    /// Pass 0 to control deactivation manually via StopScreamer().
    /// </summary>
    public void TriggerScreamer(float autoDuration = 0f)
    {
        if (screamerVCam == null)
        {
            Debug.LogWarning("[ScreamerCameraController] screamerVCam is not assigned.");
            return;
        }

        // Cancel any pending auto-stop or resume from a previous screamer
        if (autoStopCoroutine != null) { StopCoroutine(autoStopCoroutine); autoStopCoroutine = null; }
        if (resumeCoroutine != null)   { StopCoroutine(resumeCoroutine);   resumeCoroutine   = null; }

        // Freeze CM_DefaultVCam so Brain knows the exact "home" position to return to later
        defaultVCamFollower?.Freeze();

        // Pause NavigationScript so it doesn't fight Brain during the blend
        if (navigationScript != null)
            navigationScript.enabled = false;

        SetBrainBlend(blendInStyle, blendInDuration);
        screamerVCam.gameObject.SetActive(true);

        if (autoDuration > 0f)
            autoStopCoroutine = StartCoroutine(AutoStopRoutine(autoDuration));
    }

    /// <summary>Deactivates the screamer camera and smoothly returns to the normal view.</summary>
    public void StopScreamer()
    {
        if (autoStopCoroutine != null) { StopCoroutine(autoStopCoroutine); autoStopCoroutine = null; }

        SetBrainBlend(blendOutStyle, blendOutDuration);
        screamerVCam.gameObject.SetActive(false);

        // Re-enable NavigationScript and unfreeze CM_DefaultVCam after blend-out completes
        resumeCoroutine = StartCoroutine(ResumeAfterBlendOut());
    }

    private IEnumerator AutoStopRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        StopScreamer();
    }

    private IEnumerator ResumeAfterBlendOut()
    {
        yield return new WaitForSeconds(blendOutDuration);

        defaultVCamFollower?.Unfreeze();

        if (navigationScript != null)
            navigationScript.enabled = true;

        resumeCoroutine = null;
    }

    private void SetBrainBlend(CinemachineBlendDefinition.Style style, float duration)
    {
        if (brain != null)
            brain.m_DefaultBlend = new CinemachineBlendDefinition(style, duration);
    }
}
