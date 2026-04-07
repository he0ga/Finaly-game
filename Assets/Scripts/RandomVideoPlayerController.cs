using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class RandomVideoPlayerController : MonoBehaviour
{
    [Header("Объекты с VideoPlayer")]
    public VideoPlayer[] videoPlayers;

    [Header("Рендереры телевизоров")]
    public Renderer[] tvRenderers;

    [Header("Материалы телевизоров (по одному на Renderer)")]
    public Material[] tvMaterials;

    [Header("Интервал смены (сек)")]
    public float changeInterval = 5f;

    private VideoPlayer currentPlayer = null;
    private int currentIndex = -1;

    void Start()
    {
        if (videoPlayers == null || videoPlayers.Length == 0)
        {
            Debug.LogError("Нет объектов VideoPlayer!");
            return;
        }
        if (tvRenderers == null || tvMaterials == null || tvRenderers.Length != tvMaterials.Length)
        {
            Debug.LogError("Массивы Renderer и материалов должны быть заданы и одного размера!");
            return;
        }

        // Инициализация: каждому Renderer назначаем уникальный материал и выключаем видео и эмиссию
        for (int i = 0; i < tvMaterials.Length; i++)
        {
            tvMaterials[i].EnableKeyword("_EMISSION");
            tvRenderers[i].material = tvMaterials[i];
            // Выключаем эмиссию (черный цвет)
            tvMaterials[i].SetColor("_EmissionColor", Color.black);
            // Останавливаем видео
            if (i < videoPlayers.Length)
            {
                videoPlayers[i].Stop();
                videoPlayers[i].targetMaterialRenderer = tvRenderers[i];
                videoPlayers[i].targetMaterialProperty = "_EmissionColor";
            }
        }

        StartCoroutine(RandomPlayRoutine());
    }

    IEnumerator RandomPlayRoutine()
    {
        while (true)
        {
            // Останавливаем текущее видео и гасим эмиссию
            if (currentPlayer != null && currentIndex >= 0 && currentIndex < tvMaterials.Length)
            {
                currentPlayer.Stop();
                tvMaterials[currentIndex].SetColor("_EmissionColor", Color.black);
            }

            int newIndex;
            do
            {
                newIndex = Random.Range(0, videoPlayers.Length);
            }
            while (videoPlayers.Length > 1 && newIndex == currentIndex);

            currentIndex = newIndex;
            currentPlayer = videoPlayers[currentIndex];

            if (currentIndex < tvMaterials.Length)
            {
                // Включаем эмиссию белым цветом
                tvMaterials[currentIndex].SetColor("_EmissionColor", Color.white);
            }

            currentPlayer.Play();

            yield return new WaitForSeconds(changeInterval);
        }
    }
}
