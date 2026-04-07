using UnityEngine;
using UnityEngine.Video;
using System.Collections;
public class VideoScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public VideoClip firstVideo;
    public VideoClip secondVideo;

    private VideoPlayer videoPlayer;

    void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        StartCoroutine(PlayVideoSequence());
    }

    IEnumerator PlayVideoSequence()
    {
        // Воспроизводим первое видео
        videoPlayer.clip = firstVideo;
        videoPlayer.isLooping = false;
        videoPlayer.Play();

        // Ждем окончания первого видео
        yield return new WaitForSeconds((float)firstVideo.length);

        // Воспроизводим второе видео с зацикливанием
        videoPlayer.clip = secondVideo;
        videoPlayer.isLooping = true;
        videoPlayer.Play();
    }
}
