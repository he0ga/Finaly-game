using UnityEngine;

/// <summary>
/// Copies the source transform's position and rotation to this GameObject every LateUpdate.
/// Can be frozen to lock the transform at a snapshot (used while a screamer blend is active).
/// Runs at execution order -100 so it updates before CinemachineBrain's own LateUpdate.
/// </summary>
[DefaultExecutionOrder(-100)]
public class CinemachineFollowTransform : MonoBehaviour
{
    [SerializeField] private Transform source;

    private bool isFrozen;

    /// <summary>Locks the transform at its current world position/rotation.</summary>
    public void Freeze() => isFrozen = true;

    /// <summary>Resumes copying the source transform and immediately syncs.</summary>
    public void Unfreeze()
    {
        isFrozen = false;
        Sync();
    }

    private void LateUpdate()
    {
        if (!isFrozen)
            Sync();
    }

    private void Sync()
    {
        if (source == null)
            return;

        transform.SetPositionAndRotation(source.position, source.rotation);
    }
}
