using System.Collections;
using System.Linq;
using UnityEngine;

public class CharactersSystem : MonoBehaviour
{
    [Header("Точки пути налево")]
    [SerializeField] private GameObject[] pointsLeft;
    [Header("Точки пути направо")]
    [SerializeField] private GameObject[] pointsRight;
    [Header("Персонаж")]
    [SerializeField] private GameObject character;
    [Header("Высота персонажа над точкой")]
    [SerializeField] private float characterHeight = 1f;
    [Header("Камеры")]
    [SerializeField] private Camera[] cameras;
    [Header("Настройки поворота")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private bool freezeXRotation = true;
    [SerializeField] private bool freezeZRotation = true;
    [Header("Время ожидания на точке")]
    [SerializeField] private float pointWaitTimeMax = 2f;
    [SerializeField] private float pointWaitTimeMin = 0.5f;
    [Header("Время ожидания у двери")]
    [SerializeField] private float doorWaitTimeMax = 3f;
    [SerializeField] private float doorWaitTimeMin = 1f;
    [Header("Двери")]
    [SerializeField] private GameObject doorLeft;
    [SerializeField] private GameObject doorRight;
    [Header("Состояния дверей")]
    [SerializeField] private bool doorLeftClosed = true;
    [SerializeField] private bool doorRightClosed = true;
    [Header("Телепортация между точками")]
    [SerializeField] private bool useTeleportation = true;
    [SerializeField] private float teleportEffectDuration = 0.5f;
    [SerializeField] private GameObject teleportEffectPrefab;
    [SerializeField] private AudioSource knock;
    [Header("Настройки сцены")]
    [SerializeField] private GameObject Game;
    [SerializeField] private GameObject endGamePanel;

    private GameObject[] currentPathPoints;
    private int currentPointIndex = 0;
    private bool isWaiting = false;
    private Coroutine movementCoroutine;
    private bool hasReachedLastPoint = false;
    private bool chooseLeftPath;

    void Start()
    {
        ChooseNewPath();
        movementCoroutine = StartCoroutine(MovementRoutine());
    }

    void ChooseNewPath()
    {
        // Случайный выбор пути (50/50 лево/право)
        chooseLeftPath = Random.value > 0.5f;
        currentPathPoints = chooseLeftPath ? pointsLeft : pointsRight;

        if (currentPathPoints.Length == 0)
        {
            Debug.LogError("Выбранный путь пуст!");
            return;
        }

        // Рандомно выбираем позицию из первых трёх точек
        currentPointIndex = Random.Range(0, Mathf.Min(3, currentPathPoints.Length));
        TeleportToPoint(currentPointIndex);

        // Сброс флага окончания пути
        hasReachedLastPoint = false;

        // Включаем игровой объект если было выключено
        if (Game != null) Game.SetActive(true);
        if (endGamePanel != null) endGamePanel.SetActive(false);

        Debug.Log($"Выбран новый путь: {(chooseLeftPath ? "Левый" : "Правый")}, начальная точка: {currentPointIndex}");
    }

    void LateUpdate()
    {
        if (character != null)
        {
            Vector3 euler = character.transform.rotation.eulerAngles;
            character.transform.rotation = Quaternion.Euler(
                freezeXRotation ? 0 : euler.x,
                euler.y,
                freezeZRotation ? 0 : euler.z
            );
        }
    }

    IEnumerator MovementRoutine()
    {
        while (true)
        {
            if (hasReachedLastPoint)
            {
                // Минимальная задержка перед выбором нового пути
                yield return new WaitForSeconds(0.1f);
                ChooseNewPath();
                continue;
            }

            // Ждем перед движением к следующей точке
            float waitTime = Random.Range(pointWaitTimeMin, pointWaitTimeMax);
            yield return new WaitForSeconds(waitTime);

            int nextPointIndex = GetNextPointIndex();

            if (hasReachedLastPoint)
            {
                // Немедленно переходим к выбору нового пути
                continue;
            }

            if (useTeleportation)
                yield return StartCoroutine(TeleportToPointRoutine(nextPointIndex));
            else
                yield return StartCoroutine(MoveToPointRoutine(nextPointIndex));

            currentPointIndex = nextPointIndex;

            // Проверяем, достигли ли последней точки после перемещения
            if (currentPointIndex == currentPathPoints.Length - 1)
            {
                hasReachedLastPoint = true;
            }
        }
    }

    IEnumerator TeleportToPointRoutine(int pointIndex)
    {
        if (teleportEffectPrefab != null)
            Instantiate(teleportEffectPrefab, character.transform.position, Quaternion.identity);

        character.SetActive(false);
        yield return new WaitForSeconds(teleportEffectDuration / 2);

        TeleportToPoint(pointIndex);

        if (teleportEffectPrefab != null)
            Instantiate(teleportEffectPrefab, character.transform.position, Quaternion.identity);

        character.SetActive(true);
        yield return new WaitForSeconds(teleportEffectDuration / 2);
    }

    IEnumerator MoveToPointRoutine(int pointIndex)
    {
        Vector3 startPosition = character.transform.position;
        Vector3 targetPosition = GetPointPositionWithHeight(currentPathPoints[pointIndex].transform.position);
        float distance = Vector3.Distance(startPosition, targetPosition);
        float duration = Mathf.Max(0.5f, distance / 2f); // Фиксированная скорость движения
        float elapsed = 0f;

        while (elapsed < duration && !hasReachedLastPoint)
        {
            if (hasReachedLastPoint) break;

            character.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!hasReachedLastPoint)
        {
            character.transform.position = targetPosition;
        }
    }

    void TeleportToPoint(int pointIndex)
    {
        character.transform.position = GetPointPositionWithHeight(currentPathPoints[pointIndex].transform.position);
        SmoothLookAtNearestCamera(character.transform);
    }

    Vector3 GetPointPositionWithHeight(Vector3 pointPosition)
    {
        return new Vector3(pointPosition.x, pointPosition.y + characterHeight, pointPosition.z);
    }

    void SmoothLookAtNearestCamera(Transform characterTransform)
    {
        if (cameras == null || cameras.Length == 0 || characterTransform == null)
            return;

        Camera nearestCamera = cameras.OrderBy(cam =>
            Vector3.Distance(characterTransform.position, cam.transform.position)
        ).First();

        Vector3 direction = nearestCamera.transform.position - characterTransform.position;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            float targetYRotation = Quaternion.LookRotation(direction).eulerAngles.y;
            Quaternion newRotation = Quaternion.Euler(
                freezeXRotation ? 0 : characterTransform.rotation.eulerAngles.x,
                targetYRotation,
                freezeZRotation ? 0 : characterTransform.rotation.eulerAngles.z
            );
            characterTransform.rotation = newRotation;
        }
    }

    int GetNextPointIndex()
    {
        // Если это предпоследняя точка (перед дверью)
        if (currentPointIndex == currentPathPoints.Length - 2)
        {
            bool doorClosed = chooseLeftPath ? doorLeftClosed : doorRightClosed;
            GameObject currentDoor = chooseLeftPath ? doorLeft : doorRight;

            if (currentDoor != null && currentDoor.activeInHierarchy && doorClosed)
            {
                knock.Play();
                // Дверь закрыта - сразу запускаем новый путь
                hasReachedLastPoint = true;
                return currentPointIndex;
            }

            // Дверь открыта - идем к следующей точке
            return currentPointIndex + 1;
        }
        // Если это последняя точка
        else if (currentPointIndex == currentPathPoints.Length - 1)
        {
            // Достигли конца пути - запускаем новый путь
            hasReachedLastPoint = true;
            return currentPointIndex;
        }
        else
        {
            // Обычное движение к следующей точке
            return currentPointIndex + 1;
        }
    }

    IEnumerator WaitAtDoor(float waitTime)
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitTime);
        isWaiting = false;
    }

    void OnDrawGizmos()
    {
        if (currentPathPoints == null || currentPathPoints.Length < 2)
            return;

        Gizmos.color = Color.blue;

        for (int i = 0; i < currentPathPoints.Length - 1; i++)
        {
            if (currentPathPoints[i] != null && currentPathPoints[i + 1] != null)
            {
                Vector3 startPos = GetPointPositionWithHeight(currentPathPoints[i].transform.position);
                Vector3 endPos = GetPointPositionWithHeight(currentPathPoints[i + 1].transform.position);
                Gizmos.DrawLine(startPos, endPos);
            }
        }
    }
}