using UnityEngine;

/// <summary>
/// Camera smoothly follows the lamp downward only (Y axis).
/// X position stays fixed so the camera doesn't drift sideways.
/// </summary>
public class CameraFollowY : MonoBehaviour
{
    [Tooltip("The lamp Transform to follow.")]
    public Transform target;

    [Tooltip("Vertical offset above the lamp.")]
    public float offsetY = 2f;

    [Tooltip("How smoothly the camera follows. Lower = smoother.")]
    public float smoothSpeed = 5f;

    private float fixedX;
    private float fixedZ;

    private void Start()
    {
        fixedX = transform.position.x;
        fixedZ = transform.position.z;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        float targetY = target.position.y + offsetY;
        float smoothY = Mathf.Lerp(transform.position.y, targetY, smoothSpeed * Time.deltaTime);
        transform.position = new Vector3(fixedX, smoothY, fixedZ);
    }
}
