using UnityEngine;

/// <summary>
/// Place this on a trigger collider at the bottom of the tube.
/// Notifies MinigameManager when the lamp reaches the finish.
/// </summary>
public class MinigameFinish : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<LampaController>() != null)
            MinigameManager.Instance.OnFinishReached();
    }
}
