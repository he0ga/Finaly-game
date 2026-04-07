using UnityEngine;

public class Screamer : MonoBehaviour
{
    [Header("Анимация и звук скримера")]
    [SerializeField] private Animator screamerAnimator;
    [SerializeField] private AudioSource screamerAudio;
    [SerializeField] private float screamerDuration = 3f;

    private bool isScreamerActive = false;

    public void TriggerScreamer()
    {
        if (isScreamerActive) return;

        isScreamerActive = true;
        gameObject.SetActive(true);

        if (screamerAnimator != null)
        {
            screamerAnimator.SetTrigger("Scream");
        }

        if (screamerAudio != null)
        {
            screamerAudio.Play();
        }

        Invoke(nameof(DisableScreamer), screamerDuration);
    }

    void DisableScreamer()
    {
        isScreamerActive = false;
        gameObject.SetActive(false);
    }
}