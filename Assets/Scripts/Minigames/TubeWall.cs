using UnityEngine;

/// <summary>
/// Attach to each wall collider of the tube.
/// Notifies MinigameManager when the lamp touches this wall.
/// </summary>
public class TubeWall : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<LampaController>() != null)
            MinigameManager.Instance.OnWallHit();
    }
}
