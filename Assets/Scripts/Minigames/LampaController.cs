using UnityEngine;

/// <summary>
/// Controls the player-controlled lamp in MiniGame_1.
/// The lamp falls downward automatically while the player steers it horizontally.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class LampaController : MonoBehaviour
{
    [Header("Horizontal Movement")]
    [Tooltip("Maximum horizontal speed the player can steer.")]
    public float horizontalSpeed = 3f;

    [Header("Falling")]
    [Tooltip("Constant downward speed added on top of gravity.")]
    public float fallSpeed = 2f;
    [Tooltip("How fast fall speed ramps up over time.")]
    public float fallAcceleration = 0.3f;
    [Tooltip("Maximum fall speed cap.")]
    public float maxFallSpeed = 8f;

    private Rigidbody2D rb;
    private float currentFallSpeed;
    private bool isActive = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        currentFallSpeed = fallSpeed;
    }

    /// <summary>Called by MinigameManager to start / stop player control.</summary>
    public void SetActive(bool active)
    {
        isActive = active;
        if (!active)
            rb.velocity = Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (!isActive) return;

        float horizontal = Input.GetAxis("Horizontal");

        currentFallSpeed = Mathf.Min(currentFallSpeed + fallAcceleration * Time.fixedDeltaTime, maxFallSpeed);

        Vector2 velocity = new Vector2(horizontal * horizontalSpeed, -currentFallSpeed);
        rb.velocity = velocity;
    }
}
