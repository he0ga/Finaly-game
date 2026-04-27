using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class NavigationScript : MonoBehaviour
{
    [Header("Camera Reference")]
    public GameObject camera;

    [Header("Rotation Settings")]
    public float rotationDuration = 0.5f;
    public float rotationAngleLeft = 30f;
    public float rotationAngleRight = 30f;

    [Header("Rotation Curve")]
    [Tooltip("Controls how the rotation progresses over time. X = normalized time (0..1), Y = normalized progress (0..1).")]
    public AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Motion Blur")]
    [Tooltip("Maximum shutter angle applied at the peak of rotation speed.")]
    [Range(0f, 360f)]
    public float blurMaxShutterAngle = 270f;
    [Tooltip("How quickly the motion blur fades in and out.")]
    public float blurFadeSpeed = 8f;

    private float initialYRotation;
    private float currentTargetOffset;
    private Coroutine currentRotation;
    private bool isLeftPressed = false;
    private bool isRightPressed = false;

    private MotionBlur motionBlurEffect;
    private float originalShutterAngle;
    private float currentBlurTarget;

    private void Awake()
    {
        // Read the initial Y rotation from the camera's actual transform instead of overwriting it
        initialYRotation = camera.transform.eulerAngles.y;
    }

    private void Start()
    {
        // Grab MotionBlur from the PostProcessVolume on the camera
        var volume = camera.GetComponentInChildren<PostProcessVolume>();
        if (volume != null && volume.sharedProfile != null)
        {
            volume.sharedProfile.TryGetSettings(out motionBlurEffect);
            if (motionBlurEffect != null)
                originalShutterAngle = motionBlurEffect.shutterAngle.value;
        }
    }

    private void Update()
    {
        HandleKeyboardInput();
        UpdateMotionBlur();
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.A)) isLeftPressed = true;
        if (Input.GetKeyUp(KeyCode.A)) isLeftPressed = false;

        if (Input.GetKeyDown(KeyCode.D)) isRightPressed = true;
        if (Input.GetKeyUp(KeyCode.D)) isRightPressed = false;

        if (isLeftPressed && !isRightPressed)
        {
            RotateTo(-rotationAngleLeft);
        }
        else if (isRightPressed && !isLeftPressed)
        {
            RotateTo(rotationAngleRight);
        }
        else if (!isLeftPressed && !isRightPressed)
        {
            RotateTo(0);
        }
    }

    /// <summary>Smoothly drives the MotionBlur shutter angle toward the current target.</summary>
    private void UpdateMotionBlur()
    {
        if (motionBlurEffect == null)
            return;

        motionBlurEffect.shutterAngle.value = Mathf.Lerp(
            motionBlurEffect.shutterAngle.value,
            currentBlurTarget,
            blurFadeSpeed * Time.deltaTime
        );
    }

    /// <summary>Triggers rotation toward the given angle offset from the initial rotation.</summary>
    public void ButtonRotate(string direction)
    {
        switch (direction)
        {
            case "left":
                isLeftPressed = true;
                isRightPressed = false;
                break;
            case "right":
                isRightPressed = true;
                isLeftPressed = false;
                break;
            case "release":
                isLeftPressed = false;
                isRightPressed = false;
                break;
        }
    }

    private void RotateTo(float targetAngleOffset)
    {
        // Skip if we're already heading to this same target offset
        if (Mathf.Approximately(currentTargetOffset, targetAngleOffset) && currentRotation != null)
            return;

        currentTargetOffset = targetAngleOffset;

        if (currentRotation != null)
            StopCoroutine(currentRotation);

        currentRotation = StartCoroutine(SmoothRotate(targetAngleOffset));
    }

    private IEnumerator SmoothRotate(float targetAngleOffset)
    {
        float time = 0f;
        Quaternion startRotation = camera.transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(0f, initialYRotation + targetAngleOffset, 0f);

        // Apply blur at the start of rotation
        currentBlurTarget = blurMaxShutterAngle;

        while (time < rotationDuration)
        {
            time += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(time / rotationDuration);

            // Sample the animation curve for the easing value
            float curveValue = rotationCurve.Evaluate(normalizedTime);
            camera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, curveValue);

            // Scale blur by rotation speed (derivative approximation using curve delta)
            float nextCurveValue = rotationCurve.Evaluate(Mathf.Clamp01(normalizedTime + 0.01f));
            float speed = Mathf.Abs(nextCurveValue - curveValue) / 0.01f;
            currentBlurTarget = Mathf.Lerp(originalShutterAngle, blurMaxShutterAngle, speed);

            yield return null;
        }

        camera.transform.rotation = targetRotation;

        // Fade blur back out
        currentBlurTarget = originalShutterAngle;
        currentRotation = null;
    }
}