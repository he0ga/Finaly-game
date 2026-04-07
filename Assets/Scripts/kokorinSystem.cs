using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class kokorinSystem : MonoBehaviour
{
    [Header("Точки прохождения для второго персонажа")]
    [SerializeField] private GameObject[] points2;

    [Header("Второй персонаж, который будет передвигаться")]
    [SerializeField] private GameObject character2;

    [Header("Высота второго персонажа над точкой")]
    [SerializeField] private float characterHeight2 = 1f;

    [Header("Время ожидания на точке")]
    [SerializeField] private float pointWaitTimeMax2 = 2f;
    [SerializeField] private float pointWaitTimeMin2 = 0.5f;

    [Header("Настройки обнаружения")]
    [SerializeField] private float detectionAngle = 30f;
    [SerializeField] private float detectionDistance = 10f;
    [SerializeField] private LayerMask detectionLayerMask;

    [Header("Эффекты телепортации")]
    [SerializeField] private float teleportEffectDuration2 = 0.5f;
    [SerializeField] private GameObject teleportEffectPrefab2;

    [Header("Настройки фонарика")]
    [SerializeField] private Light flashlight;
    [SerializeField] private float minIntensityThreshold = 0.1f;

    [Header("Настройки поворота")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private bool freezeXRotation = true;
    [SerializeField] private bool freezeZRotation = true;

    [Header("Камеры для ориентации персонажа")]
    [SerializeField] private Camera[] cameras;

    [Header("Звуковые эффекты")]
    [SerializeField] private AudioSource leavesound;
    [SerializeField] private AudioSource waitSound;

    [Header("Настройки обнаружения")]
    [SerializeField] private float requiredIlluminationTime = 2f;
    private float currentIlluminationTime = 0f;

    [Header("Время ожидания на последней точке")]
    [SerializeField] private float lastPointWaitTimeMin = 3f;
    [SerializeField] private float lastPointWaitTimeMax = 10f;

    [Header("Настройки возвращения")]
    [SerializeField] private float minReturnTime = 5f; // Минимальное время до возвращения
    [SerializeField] private float maxReturnTime = 15f; // Максимальное время до возвращения
    [SerializeField] private float minReturnWaitTime = 1f; // Минимальное ожидание между точками при возвращении
    [SerializeField] private float maxReturnWaitTime = 3f; // Максимальное ожидание между точками при возвращении

    [Header("Настройки сцены")]
    [SerializeField] private GameObject Game;
    [SerializeField] private GameObject endGamePanel;

    private Coroutine character2Routine;
    private int currentPointIndex2 = 0;
    private bool isCharacter2RunningAway = false;
    private Transform detectionOrigin;
    private bool isDetectionActive = false;

    void Start()
    {
        detectionOrigin = flashlight != null ? flashlight.transform : Camera.main.transform;

        if (points2.Length > 0)
        {
            TeleportCharacter2ToPoint(0);
            character2Routine = StartCoroutine(Character2BehaviorLoop());
        }
    }

    void Update()
    {
        if (character2.activeSelf)
        {
            if (IsFlashlightOn() && IsCharacter2Lit())
            {
                currentIlluminationTime += Time.deltaTime;

                if (currentIlluminationTime >= requiredIlluminationTime && isDetectionActive && !isCharacter2RunningAway)
                {
                    TriggerCharacter2Escape();
                }
            }
            else
            {
                currentIlluminationTime = 0f;
            }
        }
    }

    void TriggerCharacter2Escape()
    {
        leavesound.Play();
        if (character2Routine != null)
        {
            StopCoroutine(character2Routine);
        }
        character2Routine = StartCoroutine(TeleportCharacter2Away());
        waitSound.Stop();
    }

    IEnumerator Character2BehaviorLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(pointWaitTimeMin2, pointWaitTimeMax2));
            yield return StartCoroutine(TeleportCharacter2ToPointRoutine(points2.Length - 1)); // персонаж в последнюю точку
            currentPointIndex2 = points2.Length - 1;
            isDetectionActive = true;
            float lastPointWaitTime = Random.Range(lastPointWaitTimeMin, lastPointWaitTimeMax);
            float elapsedTime = 0f;
            waitSound.Play();
            while (elapsedTime < lastPointWaitTime && isDetectionActive && !isCharacter2RunningAway)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            waitSound.Stop();

            if (!isCharacter2RunningAway)
            {
                // Если персонажа не выгнали — переходим к следующей сцене
                Game.SetActive(false);
                endGamePanel.SetActive(true);
                

                // Если хотите завершить корутину чтобы не делать MoveCharacter2Back, можно сделать yield break
                yield break;
            }
            else
            {
                // Если персонажа выгнали — двигаемся назад
                yield return StartCoroutine(MoveCharacter2Back());
            }
        }
    }

    IEnumerator MoveCharacter2Back()
    {
        for (int i = points2.Length - 2; i >= 0; i--)
        {
            yield return StartCoroutine(TeleportCharacter2ToPointRoutine(i));
            currentPointIndex2 = i;
            yield return new WaitForSeconds(Random.Range(minReturnWaitTime, maxReturnWaitTime));
        }

        isDetectionActive = false;
    }

    IEnumerator TeleportCharacter2Away()
    {
        isCharacter2RunningAway = true;
        isDetectionActive = false;

        if (teleportEffectPrefab2 != null)
        {
            Instantiate(teleportEffectPrefab2, character2.transform.position, Quaternion.identity);
        }
        yield return new WaitForSeconds(teleportEffectDuration2 / 2);

        character2.SetActive(false);
        character2.transform.position = GetPointPositionWithHeight2(points2[0].transform.position);
        currentPointIndex2 = 0;

        yield return new WaitForSeconds(Random.Range(minReturnTime, maxReturnTime));

        isCharacter2RunningAway = false;
        character2Routine = StartCoroutine(Character2BehaviorLoop());
    }

    bool IsFlashlightOn()
    {
        return flashlight != null &&
               flashlight.gameObject.activeInHierarchy &&
               flashlight.enabled &&
               flashlight.intensity > minIntensityThreshold;
    }

    bool IsCharacter2Lit()
    {
        Vector3 toCharacter = character2.transform.position - detectionOrigin.position;
        float distance = toCharacter.magnitude;
        float angle = Vector3.Angle(detectionOrigin.forward, toCharacter);

        if (angle <= detectionAngle / 2f && distance <= detectionDistance)
        {
            RaycastHit hit;
            if (Physics.Raycast(detectionOrigin.position, toCharacter.normalized, out hit, distance, detectionLayerMask))
            {
                return hit.transform == character2.transform;
            }
        }
        return false;
    }

    IEnumerator TeleportCharacter2ToPointRoutine(int pointIndex)
    {
        if (teleportEffectPrefab2 != null)
        {
            Instantiate(teleportEffectPrefab2, character2.transform.position, Quaternion.identity);
        }
        character2.SetActive(false);

        yield return new WaitForSeconds(teleportEffectDuration2 / 2);

        TeleportCharacter2ToPoint(pointIndex);

        if (teleportEffectPrefab2 != null)
        {
            Instantiate(teleportEffectPrefab2, character2.transform.position, Quaternion.identity);
        }
        character2.SetActive(true);

        yield return new WaitForSeconds(teleportEffectDuration2 / 2);
    }

    void TeleportCharacter2ToPoint(int pointIndex)
    {
        if (points2.Length == 0 || pointIndex >= points2.Length) return;

        character2.transform.position = GetPointPositionWithHeight2(points2[pointIndex].transform.position);
        SmoothLookAtNearestCamera(character2.transform);
    }

    void SmoothLookAtNearestCamera(Transform characterTransform)
    {
        if (cameras == null || cameras.Length == 0 || characterTransform == null) return;

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

    Vector3 GetPointPositionWithHeight2(Vector3 pointPosition)
    {
        return new Vector3(pointPosition.x, pointPosition.y + characterHeight2, pointPosition.z);
    }

    void OnDrawGizmosSelected()
    {
        if (points2 != null && points2.Length > 1)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < points2.Length - 1; i++)
            {
                if (points2[i] != null && points2[i + 1] != null)
                {
                    Vector3 startPos = GetPointPositionWithHeight2(points2[i].transform.position);
                    Vector3 endPos = GetPointPositionWithHeight2(points2[i + 1].transform.position);
                    Gizmos.DrawLine(startPos, endPos);
                }
            }
        }

        if (detectionOrigin != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = detectionOrigin.position + detectionOrigin.forward * detectionDistance;
            float radius = Mathf.Tan(detectionAngle * Mathf.Deg2Rad / 2) * detectionDistance;
            Gizmos.DrawWireSphere(center, radius);
            Gizmos.DrawLine(detectionOrigin.position, detectionOrigin.position + detectionOrigin.forward * detectionDistance);
        }
    }
}