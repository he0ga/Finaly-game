using UnityEngine;

/// <summary>
/// Controls the player-controlled lamp in MiniGame_1.
/// The lamp falls downward automatically while the player steers it horizontally.
/// Includes floaty input inertia and random horizontal drift.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class LampaController : MonoBehaviour
{
    [Header("Horizontal Movement")]
    [Tooltip("Maximum horizontal speed the player can steer.")]
    public float horizontalSpeed = 3f;

    [Header("Floaty Controls")]
    [Tooltip("How quickly the lamp responds to input. Lower = more floaty/sluggish.")]
    [Range(0.5f, 20f)]
    public float inputSmoothing = 4f;
    [Tooltip("How much the lamp retains horizontal momentum when input is released. " +
             "Higher = slides further after letting go.")]
    [Range(0f, 1f)]
    public float inertia = 0.92f;

    [Header("Drift")]
    [Tooltip("Master intensity of all drift forces. 0 = no drift at all.")]
    [Range(0f, 1f)]
    public float driftIntensity = 0.5f;
    [Tooltip("Maximum random horizontal force applied to the lamp.")]
    public float driftStrength = 1.5f;
    [Tooltip("How often (seconds) the drift direction changes.")]
    public float driftChangeInterval = 1.2f;
    [Tooltip("How smoothly the drift force transitions between directions.")]
    [Range(0.5f, 10f)]
    public float driftSmoothing = 2f;

    [Header("Tilt")]
    [Tooltip("Maximum rotation angle in degrees along the Z axis.")]
    public float maxTiltAngle = 20f;
    [Tooltip("How fast the tilt follows the movement direction. Lower = lazier lean.")]
    [Range(1f, 20f)]
    public float tiltSmoothing = 6f;

    [Header("Falling")]
    [Tooltip("Initial downward speed.")]
    public float fallSpeed = 2f;
    [Tooltip("How fast fall speed ramps up over time.")]
    public float fallAcceleration = 0.3f;
    [Tooltip("Maximum fall speed cap.")]
    public float maxFallSpeed = 8f;

    private Rigidbody2D rb;
    private float currentFallSpeed;
    private bool isActive = false;

    // Floaty state
    private float smoothedHorizontal = 0f;

    // Drift state
    private float driftTarget = 0f;
    private float currentDrift = 0f;
    private float driftTimer = 0f;

    // Tilt state
    private float currentTiltAngle = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        // Rotation is driven manually via transform.rotation — do not freeze it.
        rb.constraints = RigidbodyConstraints2D.None;
        currentFallSpeed = fallSpeed;
        PickNewDriftTarget();
    }

    /// <summary>Called by MinigameManager to start / stop player control.</summary>
    public void SetActive(bool active)
    {
        isActive = active;
        if (!active)
        {
            rb.velocity = Vector2.zero;
            smoothedHorizontal = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (!isActive) return;

        float dt = Time.fixedDeltaTime;

        // ── Floaty input ──────────────────────────────────────────────────────
        float rawInput = Input.GetAxis("Horizontal");

        if (Mathf.Abs(rawInput) > 0.01f)
        {
            // Steering: gradually accelerate toward target input
            smoothedHorizontal = Mathf.Lerp(smoothedHorizontal, rawInput, inputSmoothing * dt);
        }
        else
        {
            // No input: bleed off momentum via inertia
            smoothedHorizontal *= Mathf.Pow(inertia, dt * 60f);
        }

        // ── Drift ─────────────────────────────────────────────────────────────
        UpdateDrift(dt);

        // ── Fall speed ────────────────────────────────────────────────────────
        currentFallSpeed = Mathf.Min(currentFallSpeed + fallAcceleration * dt, maxFallSpeed);

        float horizontalVelocity = smoothedHorizontal * horizontalSpeed
                                 + currentDrift * driftIntensity;

        rb.velocity = new Vector2(horizontalVelocity, -currentFallSpeed);

        // ── Tilt ──────────────────────────────────────────────────────────────
        UpdateTilt(horizontalVelocity, dt);
    }

    private void UpdateTilt(float horizontalVelocity, float dt)
    {
        // Normalise velocity against max possible horizontal speed so tilt
        // scales proportionally regardless of individual speed settings.
        float maxPossibleSpeed = horizontalSpeed + driftStrength * driftIntensity;
        float normalised = maxPossibleSpeed > 0f
            ? Mathf.Clamp(horizontalVelocity / maxPossibleSpeed, -1f, 1f)
            : 0f;

        // Negative Z rotation tilts right when moving right (screen-space).
        float targetAngle = -normalised * maxTiltAngle;
        currentTiltAngle = Mathf.Lerp(currentTiltAngle, targetAngle, tiltSmoothing * dt);

        transform.rotation = Quaternion.Euler(0f, 0f, currentTiltAngle);
    }

    private void UpdateDrift(float dt)
    {
        if (driftIntensity <= 0f)
        {
            currentDrift = 0f;
            return;
        }

        driftTimer -= dt;
        if (driftTimer <= 0f)
            PickNewDriftTarget();

        currentDrift = Mathf.Lerp(currentDrift, driftTarget, driftSmoothing * dt);
    }

    private void PickNewDriftTarget()
    {
        driftTarget = Random.Range(-driftStrength, driftStrength);
        driftTimer = driftChangeInterval * Random.Range(0.7f, 1.3f);
    }
}
