using System.Collections;
using System.Linq;
using UnityEngine;

public class kokorinSystem : MonoBehaviour
{
    [Header("Маршрут")]
    [Tooltip("Точки маршрута. Индекс 0 = стартовая позиция, последний = точка атаки у камеры.")]
    [SerializeField] private GameObject[] points2;

    [Header("Персонаж")]
    [Tooltip("GameObject персонажа Кокорина.")]
    [SerializeField] private GameObject character2;
    [Tooltip("Смещение по оси Y от позиции точки. Подбирается под высоту модели.")]
    [SerializeField] [Min(0f)] private float characterHeight2 = 1f;

    [Header("Ожидание в точках")]
    [Tooltip("Минимальное время (сек) перед движением к следующей точке.")]
    [SerializeField] [Min(0f)] private float pointWaitTimeMin2 = 15f;
    [Tooltip("Максимальное время (сек) перед движением к следующей точке.")]
    [SerializeField] [Min(0f)] private float pointWaitTimeMax2 = 50f;

    [Header("Детекция фонариком")]
    [Tooltip("Угол конуса обнаружения (в градусах). Персонаж убегает только если попадает в этот конус.")]
    [SerializeField] [Range(1f, 180f)] private float detectionAngle = 20f;
    [Tooltip("Максимальная дистанция обнаружения фонариком.")]
    [SerializeField] [Min(0f)] private float detectionDistance = 30f;
    [Tooltip("LayerMask для Raycast проверки видимости. Должен включать слой персонажа.")]
    [SerializeField] private LayerMask detectionLayerMask;
    [Tooltip("Источник света (фонарик). Если не задан — используется Camera.main.")]
    [SerializeField] private Light flashlight;
    [Tooltip("Минимальная интенсивность фонарика при которой считается что он включён.")]
    [SerializeField] [Min(0f)] private float minIntensityThreshold = 0.1f;
    [Tooltip("Время (сек) непрерывного освещения фонариком необходимое для побега персонажа.")]
    [SerializeField] [Min(0f)] private float requiredIlluminationTime = 1f;

    [Header("Телепортация")]
    [Tooltip("Длительность (сек) анимации телепортации между точками.")]
    [SerializeField] [Min(0f)] private float teleportEffectDuration2 = 0.5f;
    [Tooltip("Опциональный визуальный эффект (Prefab) при телепортации.")]
    [SerializeField] private GameObject teleportEffectPrefab2;

    [Header("Поворот персонажа")]
    [Tooltip("Скорость поворота к камере.")]
    [SerializeField] [Min(0.1f)] private float rotationSpeed = 5f;
    [Tooltip("Заморозить вращение по оси X.")]
    [SerializeField] private bool freezeXRotation = false;
    [Tooltip("Заморозить вращение по оси Z.")]
    [SerializeField] private bool freezeZRotation = false;

    [Header("Камеры")]
    [Tooltip("Камеры, на ближайшую из которых персонаж поворачивается при появлении.")]
    [SerializeField] private Camera[] cameras;

    [Header("Звуки")]
    [Tooltip("Звук когда персонаж уходит после обнаружения фонариком.")]
    [SerializeField] private AudioSource leavesound;
    [Tooltip("Звук пока персонаж стоит на последней точке и ждёт.")]
    [SerializeField] private AudioSource waitSound;

    [Header("Ожидание на финальной точке")]
    [Tooltip("Минимальное время (сек) стояния на последней точке перед Game Over.")]
    [SerializeField] [Min(0f)] private float lastPointWaitTimeMin = 10f;
    [Tooltip("Максимальное время (сек) стояния на последней точке перед Game Over.")]
    [SerializeField] [Min(0f)] private float lastPointWaitTimeMax = 30f;

    [Header("Возврат после побега")]
    [Tooltip("Минимальное время (сек) до повторного появления после побега.")]
    [SerializeField] [Min(0f)] private float minReturnTime = 15f;
    [Tooltip("Максимальное время (сек) до повторного появления после побега.")]
    [SerializeField] [Min(0f)] private float maxReturnTime = 60f;
    [Tooltip("Минимальная пауза (сек) между шагами при возврате к стартовой точке.")]
    [SerializeField] [Min(0f)] private float minReturnWaitTime = 10f;
    [Tooltip("Максимальная пауза (сек) между шагами при возврате к стартовой точке.")]
    [SerializeField] [Min(0f)] private float maxReturnWaitTime = 30f;

    [Header("Управление игрой")]
    [Tooltip("Корневой GameObject сцены — скрывается при Game Over.")]
    [SerializeField] private GameObject Game;
    [Tooltip("Панель Game Over — показывается при проигрыше.")]
    [SerializeField] private GameObject endGamePanel;

    private float currentIlluminationTime = 0f;
    private Coroutine character2Routine;
    private int currentPointIndex2 = 0;
    private bool isCharacter2RunningAway = false;
    private Transform detectionOrigin;
    private bool isDetectionActive = false;

    private void Start()
    {
        detectionOrigin = flashlight != null ? flashlight.transform : Camera.main.transform;

        if (points2.Length > 0)
        {
            TeleportCharacter2ToPoint(0);
            character2Routine = StartCoroutine(Character2BehaviorLoop());
        }
    }

    private void Update()
    {
        if (!character2.activeSelf) return;

        if (IsFlashlightOn() && IsCharacter2Lit())
        {
            currentIlluminationTime += Time.deltaTime;

            if (currentIlluminationTime >= requiredIlluminationTime && isDetectionActive && !isCharacter2RunningAway)
                TriggerCharacter2Escape();
        }
        else
        {
            currentIlluminationTime = 0f;
        }
    }

    private void TriggerCharacter2Escape()
    {
        if (leavesound != null) leavesound.Play();
        if (character2Routine != null) StopCoroutine(character2Routine);
        if (waitSound != null) waitSound.Stop();
        character2Routine = StartCoroutine(TeleportCharacter2Away());
    }

    private IEnumerator Character2BehaviorLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(pointWaitTimeMin2, pointWaitTimeMax2));
            yield return StartCoroutine(TeleportCharacter2ToPointRoutine(points2.Length - 1));
            currentPointIndex2 = points2.Length - 1;
            isDetectionActive = true;

            float lastPointWaitTime = Random.Range(lastPointWaitTimeMin, lastPointWaitTimeMax);
            float elapsedTime = 0f;
            if (waitSound != null) waitSound.Play();

            while (elapsedTime < lastPointWaitTime && isDetectionActive && !isCharacter2RunningAway)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (waitSound != null) waitSound.Stop();

            if (!isCharacter2RunningAway)
            {
                // Персонаж не прогнан — Game Over
                if (Game != null) Game.SetActive(false);
                if (endGamePanel != null) endGamePanel.SetActive(true);
                yield break;
            }
            else
            {
                yield return StartCoroutine(MoveCharacter2Back());
            }
        }
    }

    private IEnumerator MoveCharacter2Back()
    {
        for (int i = points2.Length - 2; i >= 0; i--)
        {
            yield return StartCoroutine(TeleportCharacter2ToPointRoutine(i));
            currentPointIndex2 = i;
            yield return new WaitForSeconds(Random.Range(minReturnWaitTime, maxReturnWaitTime));
        }

        isDetectionActive = false;
        isCharacter2RunningAway = false; // сброс флага после полного возврата
    }

    private IEnumerator TeleportCharacter2Away()
    {
        isCharacter2RunningAway = true;
        isDetectionActive = false;

        if (teleportEffectPrefab2 != null)
            Instantiate(teleportEffectPrefab2, character2.transform.position, Quaternion.identity);

        yield return new WaitForSeconds(teleportEffectDuration2 / 2f);

        character2.SetActive(false);
        character2.transform.position = GetPointPositionWithHeight2(points2[0].transform.position);
        currentPointIndex2 = 0;

        yield return new WaitForSeconds(Random.Range(minReturnTime, maxReturnTime));

        character2.SetActive(true);
        isCharacter2RunningAway = false;
        character2Routine = StartCoroutine(Character2BehaviorLoop());
    }

    private bool IsFlashlightOn()
        => flashlight != null
        && flashlight.gameObject.activeInHierarchy
        && flashlight.enabled
        && flashlight.intensity > minIntensityThreshold;

    private bool IsCharacter2Lit()
    {
        Vector3 toCharacter = character2.transform.position - detectionOrigin.position;
        float distance = toCharacter.magnitude;
        float angle = Vector3.Angle(detectionOrigin.forward, toCharacter);

        if (angle <= detectionAngle / 2f && distance <= detectionDistance)
        {
            if (Physics.Raycast(detectionOrigin.position, toCharacter.normalized, out RaycastHit hit, distance, detectionLayerMask))
                return hit.transform == character2.transform;
        }

        return false;
    }

    private IEnumerator TeleportCharacter2ToPointRoutine(int pointIndex)
    {
        if (teleportEffectPrefab2 != null)
            Instantiate(teleportEffectPrefab2, character2.transform.position, Quaternion.identity);

        character2.SetActive(false);
        yield return new WaitForSeconds(teleportEffectDuration2 / 2f);

        TeleportCharacter2ToPoint(pointIndex);

        if (teleportEffectPrefab2 != null)
            Instantiate(teleportEffectPrefab2, character2.transform.position, Quaternion.identity);

        character2.SetActive(true);
        yield return new WaitForSeconds(teleportEffectDuration2 / 2f);
    }

    private void TeleportCharacter2ToPoint(int pointIndex)
    {
        if (points2.Length == 0 || pointIndex >= points2.Length) return;
        character2.transform.position = GetPointPositionWithHeight2(points2[pointIndex].transform.position);
        LookAtNearestCamera(character2.transform);
    }

    /// <summary>Поворачивает персонажа лицом к ближайшей камере из списка.</summary>
    private void LookAtNearestCamera(Transform t)
    {
        if (cameras == null || cameras.Length == 0 || t == null) return;

        Camera nearest = cameras
            .Where(c => c != null)
            .OrderBy(c => Vector3.Distance(t.position, c.transform.position))
            .FirstOrDefault();

        if (nearest == null) return;

        Vector3 dir = nearest.transform.position - t.position;
        dir.y = 0f;

        if (dir != Vector3.zero)
        {
            float targetY = Quaternion.LookRotation(dir).eulerAngles.y;
            t.rotation = Quaternion.Euler(
                freezeXRotation ? 0f : t.rotation.eulerAngles.x,
                targetY,
                freezeZRotation ? 0f : t.rotation.eulerAngles.z
            );
        }
    }

    private Vector3 GetPointPositionWithHeight2(Vector3 p)
        => new Vector3(p.x, p.y + characterHeight2, p.z);

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        // Путь персонажа
        if (points2 != null && points2.Length > 1)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < points2.Length; i++)
            {
                if (points2[i] == null) continue;
                Vector3 pos = points2[i].transform.position + Vector3.up * characterHeight2;
                Gizmos.DrawWireSphere(pos, 0.25f);
                if (i < points2.Length - 1 && points2[i + 1] != null)
                    Gizmos.DrawLine(pos, points2[i + 1].transform.position + Vector3.up * characterHeight2);
            }
        }

        // Конус детекции фонарика
        if (detectionOrigin != null)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
            Vector3 center = detectionOrigin.position + detectionOrigin.forward * detectionDistance;
            float radius = Mathf.Tan(detectionAngle * Mathf.Deg2Rad / 2f) * detectionDistance;
            Gizmos.DrawWireSphere(center, radius);
            Gizmos.DrawLine(detectionOrigin.position, center);
        }
    }
}