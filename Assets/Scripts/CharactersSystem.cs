using System.Collections;
using System.Linq;
using UnityEngine;

public class CharactersSystem : MonoBehaviour
{
    [Header("Путь — Левая сторона")]
    [Tooltip("Точки левого коридора. Индекс 0 = старт, последний индекс = финальная точка у двери.")]
    [SerializeField] private GameObject[] pointsLeft;

    [Header("Путь — Правая сторона")]
    [Tooltip("Точки правого коридора. Индекс 0 = старт, последний индекс = финальная точка у двери.")]
    [SerializeField] private GameObject[] pointsRight;

    [Header("Персонаж")]
    [Tooltip("GameObject персонажа, который перемещается по маршруту.")]
    [SerializeField] private GameObject character;
    [Tooltip("Смещение по оси Y от позиции точки. Подбирается под высоту модели.")]
    [SerializeField] [Min(0f)] private float characterHeight = 1f;

    [Header("Камеры")]
    [Tooltip("Камеры, на ближайшую из которых персонаж поворачивается при появлении в точке.")]
    [SerializeField] private Camera[] cameras;

    [Header("Поворот персонажа")]
    [Tooltip("Скорость плавного поворота (используется при движении без телепортации).")]
    [SerializeField] [Min(0.1f)] private float rotationSpeed = 5f;
    [Tooltip("Заморозить вращение по оси X (рекомендуется для 3D-персонажей на плоском полу).")]
    [SerializeField] private bool freezeXRotation = true;
    [Tooltip("Заморозить вращение по оси Z.")]
    [SerializeField] private bool freezeZRotation = true;

    [Header("Ожидание в точках")]
    [Tooltip("Минимальное время ожидания (сек) перед переходом к следующей точке.")]
    [SerializeField] [Min(0f)] private float pointWaitTimeMin = 5f;
    [Tooltip("Максимальное время ожидания (сек) перед переходом к следующей точке.")]
    [SerializeField] [Min(0f)] private float pointWaitTimeMax = 15f;

    [Header("Ожидание у двери")]
    [Tooltip("Минимальное время (сек) стука в закрытую дверь перед отступлением.")]
    [SerializeField] [Min(0f)] private float doorWaitTimeMin = 5f;
    [Tooltip("Максимальное время (сек) стука в закрытую дверь перед отступлением.")]
    [SerializeField] [Min(0f)] private float doorWaitTimeMax = 10f;

    [Header("Двери")]
    [Tooltip("GameObject левой двери. Персонаж проверяет, активна ли она перед входом.")]
    [SerializeField] private GameObject doorLeft;
    [Tooltip("GameObject правой двери.")]
    [SerializeField] private GameObject doorRight;
    [Tooltip("Заблокирована ли левая дверь по умолчанию?")]
    [SerializeField] private bool doorLeftClosed = true;
    [Tooltip("Заблокирована ли правая дверь по умолчанию?")]
    [SerializeField] private bool doorRightClosed = true;

    [Header("Телепортация")]
    [Tooltip("Вкл = мгновенное перемещение между точками. Выкл = плавное движение.")]
    [SerializeField] private bool useTeleportation = true;
    [Tooltip("Суммарная длительность (сек) анимации телепортации (исчезание + появление).")]
    [SerializeField] [Min(0f)] private float teleportEffectDuration = 0f;
    [Tooltip("Опциональный визуальный эффект (Prefab), спавнится при телепортации.")]
    [SerializeField] private GameObject teleportEffectPrefab;

    [Header("Звуки")]
    [Tooltip("AudioSource воспроизводимый при стуке в дверь.")]
    [SerializeField] private AudioSource knock;

    [Header("Управление игрой")]
    [Tooltip("Корневой GameObject сцены — скрывается при Game Over.")]
    [SerializeField] private GameObject Game;
    [Tooltip("Панель Game Over — показывается при проигрыше.")]
    [SerializeField] private GameObject endGamePanel;

    private GameObject[] currentPathPoints;
    private int currentPointIndex = 0;
    private Coroutine movementCoroutine;
    private bool hasReachedLastPoint = false;
    private bool chooseLeftPath;

    private void Start()
    {
        ChooseNewPath();
        movementCoroutine = StartCoroutine(MovementRoutine());
    }

    private void LateUpdate()
    {
        if (character == null) return;
        Vector3 euler = character.transform.rotation.eulerAngles;
        character.transform.rotation = Quaternion.Euler(
            freezeXRotation ? 0f : euler.x,
            euler.y,
            freezeZRotation ? 0f : euler.z
        );
    }

    private void ChooseNewPath()
    {
        chooseLeftPath = Random.value > 0.5f;
        currentPathPoints = chooseLeftPath ? pointsLeft : pointsRight;

        if (currentPathPoints == null || currentPathPoints.Length == 0)
        {
            Debug.LogError($"[CharactersSystem] Массив точек пуст для пути: {(chooseLeftPath ? "Левый" : "Правый")}");
            return;
        }

        currentPointIndex = Random.Range(0, Mathf.Min(3, currentPathPoints.Length));
        TeleportToPoint(currentPointIndex);
        hasReachedLastPoint = false;

        if (Game != null) Game.SetActive(true);
        if (endGamePanel != null) endGamePanel.SetActive(false);
    }

    private IEnumerator MovementRoutine()
    {
        while (true)
        {
            if (hasReachedLastPoint)
            {
                yield return new WaitForSeconds(0.1f);

                if (currentPointIndex == currentPathPoints.Length - 1)
                {
                    TriggerGameOver();
                    yield break;
                }

                ChooseNewPath();
                continue;
            }

            float waitTime = Random.Range(pointWaitTimeMin, pointWaitTimeMax);
            yield return new WaitForSeconds(waitTime);

            int nextPointIndex = GetNextPointIndex();
            if (hasReachedLastPoint) continue;

            if (useTeleportation)
                yield return StartCoroutine(TeleportToPointRoutine(nextPointIndex));
            else
                yield return StartCoroutine(MoveToPointRoutine(nextPointIndex));

            currentPointIndex = nextPointIndex;

            if (currentPointIndex == currentPathPoints.Length - 1)
                hasReachedLastPoint = true;
        }
    }

    /// <summary>Включает панель Game Over и скрывает игровые объекты.</summary>
    private void TriggerGameOver()
    {
        if (Game != null) Game.SetActive(false);
        if (endGamePanel != null) endGamePanel.SetActive(true);
    }

    private IEnumerator TeleportToPointRoutine(int pointIndex)
    {
        if (teleportEffectPrefab != null)
            Instantiate(teleportEffectPrefab, character.transform.position, Quaternion.identity);

        character.SetActive(false);
        yield return new WaitForSeconds(teleportEffectDuration / 2f);

        TeleportToPoint(pointIndex);

        if (teleportEffectPrefab != null)
            Instantiate(teleportEffectPrefab, character.transform.position, Quaternion.identity);

        character.SetActive(true);
        yield return new WaitForSeconds(teleportEffectDuration / 2f);
    }

    private IEnumerator MoveToPointRoutine(int pointIndex)
    {
        Vector3 startPosition = character.transform.position;
        Vector3 targetPosition = GetPointPositionWithHeight(currentPathPoints[pointIndex].transform.position);
        float distance = Vector3.Distance(startPosition, targetPosition);
        float duration = Mathf.Max(0.5f, distance / 2f);
        float elapsed = 0f;

        while (elapsed < duration && !hasReachedLastPoint)
        {
            character.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!hasReachedLastPoint)
            character.transform.position = targetPosition;
    }

    private void TeleportToPoint(int pointIndex)
    {
        character.transform.position = GetPointPositionWithHeight(currentPathPoints[pointIndex].transform.position);
        LookAtNearestCamera(character.transform);
    }

    private Vector3 GetPointPositionWithHeight(Vector3 p)
        => new Vector3(p.x, p.y + characterHeight, p.z);

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

    private int GetNextPointIndex()
    {
        if (currentPointIndex == currentPathPoints.Length - 2)
        {
            bool doorClosed = chooseLeftPath ? doorLeftClosed : doorRightClosed;
            GameObject currentDoor = chooseLeftPath ? doorLeft : doorRight;

            if (currentDoor != null && currentDoor.activeInHierarchy && doorClosed)
            {
                if (knock != null) knock.Play();
                hasReachedLastPoint = true;
                return currentPointIndex;
            }

            return currentPointIndex + 1;
        }

        if (currentPointIndex == currentPathPoints.Length - 1)
        {
            hasReachedLastPoint = true;
            return currentPointIndex;
        }

        return currentPointIndex + 1;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        DrawPathGizmo(pointsLeft, Color.cyan);
        DrawPathGizmo(pointsRight, Color.red);
    }

    private void DrawPathGizmo(GameObject[] points, Color color)
    {
        if (points == null || points.Length == 0) return;
        Gizmos.color = color;
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null) continue;
            Vector3 pos = points[i].transform.position + Vector3.up * characterHeight;
            Gizmos.DrawWireSphere(pos, 0.25f);
            if (i < points.Length - 1 && points[i + 1] != null)
                Gizmos.DrawLine(pos, points[i + 1].transform.position + Vector3.up * characterHeight);
        }
    }
}