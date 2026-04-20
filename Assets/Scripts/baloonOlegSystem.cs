using System.Collections;
using UnityEngine;

public class baloonOlegSystem : MonoBehaviour
{
    [Header("Маршрут")]
    [Tooltip("Точки появления персонажа. Выбирается случайно при каждом появлении.")]
    [SerializeField] private GameObject[] points;

    [Header("Персонаж")]
    [Tooltip("GameObject персонажа Балун-Олега.")]
    [SerializeField] private GameObject character;
    [Tooltip("Смещение по оси Y от позиции точки. Подбирается под высоту модели.")]
    [SerializeField] [Min(0f)] private float characterHeight = 1f;

    [Header("Камера")]
    [Tooltip("Камера, на которую персонаж поворачивается при появлении.")]
    [SerializeField] private Camera camera;

    [Header("Поворот персонажа")]
    [SerializeField] [Min(0.1f)] private float rotationSpeed = 5f;
    [Tooltip("Заморозить вращение по оси X.")]
    [SerializeField] private bool freezeXRotation = true;
    [Tooltip("Заморозить вращение по оси Z.")]
    [SerializeField] private bool freezeZRotation = true;

    [Header("Ожидание в точке")]
    [Tooltip("Минимальное время (сек) ожидания персонажа на точке. За это время игрок должен его прогнать.")]
    [SerializeField] [Min(0f)] private float pointWaitTimeMin = 0.5f;
    [Tooltip("Максимальное время (сек) ожидания на точке.")]
    [SerializeField] [Min(0f)] private float pointWaitTimeMax = 2f;

    [Header("Погоня")]
    [Tooltip("Минимальное время (сек) анимации погони после попытки игрока прогнать персонажа.")]
    [SerializeField] [Min(0f)] private float minChaseTime = 1f;
    [Tooltip("Максимальное время (сек) анимации погони.")]
    [SerializeField] [Min(0f)] private float maxChaseTime = 3f;

    [Header("Анимация рук")]
    [Tooltip("GameObject с Animator рук игрока. Проверяет bool 'isActive' чтобы понять, отогнал ли игрок персонажа.")]
    [SerializeField] private GameObject handAnimator;

    [Header("Первое появление")]
    [Tooltip("Минимальная задержка (сек) перед первым появлением после старта.")]
    [SerializeField] [Min(0f)] private float minFirstAppearanceTime = 10f;
    [Tooltip("Максимальная задержка (сек) перед первым появлением.")]
    [SerializeField] [Min(0f)] private float maxFirstAppearanceTime = 20f;

    [Header("Возврат после исчезновения")]
    [Tooltip("Минимальное время (сек) ожидания перед повторным появлением.")]
    [SerializeField] [Min(0f)] private float minReturnTime = 10f;
    [Tooltip("Максимальное время (сек) ожидания перед повторным появлением.")]
    [SerializeField] [Min(0f)] private float maxReturnTime = 20f;

    [Header("Пауза между стадиями звука")]
    [Tooltip("Минимальная пауза (сек) между воспроизведением звуков стадий.")]
    [SerializeField] [Min(0f)] private float minStageWait = 10f;
    [Tooltip("Максимальная пауза (сек) между воспроизведением звуков стадий.")]
    [SerializeField] [Min(0f)] private float maxStageWait = 50f;

    [Header("Звуки стадий")]
    [Tooltip("Звук первой стадии — персонаж далеко.")]
    [SerializeField] private AudioClip stage1Sound;
    [Tooltip("Звук второй стадии — персонаж приближается.")]
    [SerializeField] private AudioClip stage2Sound;
    [Tooltip("Звук третьей стадии — персонаж рядом, сейчас появится.")]
    [SerializeField] private AudioClip stage3Sound;
    [Tooltip("Звук когда игрок успешно прогоняет персонажа.")]
    [SerializeField] private AudioClip leaveSound;
    [Tooltip("Звук когда игрок НЕ успевает прогнать персонажа и теряет заряд батареи.")]
    [SerializeField] private AudioClip noleaveSound;

    [Header("Настройки AudioSource")]
    [Tooltip("Громкость воспроизводимых звуков.")]
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private bool loop = false;
    [SerializeField] [Range(0.1f, 3f)] private float pitch = 1f;
    [SerializeField] [Range(0, 256)] private int priority = 128;
    [SerializeField] private bool mute = false;

    [Header("Батарея")]
    [Tooltip("Ссылка на систему батареи. Если не задана — дрейн батареи не применяется.")]
    [SerializeField] private BatterySystem batterySystem;
    [Tooltip("Количество заряда, снимаемое с батареи если игрок не успел прогнать персонажа.")]
    [SerializeField] [Min(0f)] private float batteryDrainAmount = 10f;

    private AudioSource audioSource;
    private Animator characterAnimator;
    private Animator handAnim;

    private void Start()
    {
        characterAnimator = character.GetComponent<Animator>();

        if (handAnimator != null)
            handAnim = handAnimator.GetComponent<Animator>();
        else
            Debug.LogWarning("[baloonOlegSystem] Hand Animator не назначен — погоня определяться не будет.");

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.volume   = volume;
        audioSource.loop     = loop;
        audioSource.pitch    = pitch;
        audioSource.priority = priority;
        audioSource.mute     = mute;

        HideCharacter();
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(Random.Range(minFirstAppearanceTime, maxFirstAppearanceTime));
        StartCoroutine(CharacterRoutine());
    }

    private IEnumerator CharacterRoutine()
    {
        while (true)
        {
            // Стадия 1
            if (stage1Sound != null)
            {
                audioSource.PlayOneShot(stage1Sound);
                yield return new WaitForSeconds(stage1Sound.length);
            }
            yield return new WaitForSeconds(Random.Range(minStageWait, maxStageWait));

            // Стадия 2
            if (stage2Sound != null)
            {
                audioSource.PlayOneShot(stage2Sound);
                yield return new WaitForSeconds(stage2Sound.length);
            }
            yield return new WaitForSeconds(Random.Range(minStageWait, maxStageWait));

            // Стадия 3
            if (stage3Sound != null)
            {
                audioSource.PlayOneShot(stage3Sound);
                yield return new WaitForSeconds(stage3Sound.length);
            }

            // Персонаж появляется в случайной точке
            GameObject randomPoint = points[Random.Range(0, points.Length)];
            character.transform.position = randomPoint.transform.position + Vector3.up * characterHeight;
            if (characterAnimator != null) characterAnimator.SetBool("isActive", true);

            float waitTime = Random.Range(pointWaitTimeMin, pointWaitTimeMax);
            float timer = 0f;
            bool isChased = false;

            while (timer < waitTime && !isChased)
            {
                timer += Time.deltaTime;

                // Поворот к камере
                if (camera != null)
                {
                    Vector3 dirToCam = camera.transform.position - character.transform.position;
                    Quaternion lookRot = Quaternion.LookRotation(dirToCam);
                    Vector3 euler = lookRot.eulerAngles;
                    if (freezeXRotation) euler.x = 0f;
                    if (freezeZRotation) euler.z = 0f;
                    character.transform.rotation = Quaternion.Slerp(
                        character.transform.rotation,
                        Quaternion.Euler(euler),
                        rotationSpeed * Time.deltaTime
                    );
                }

                if (handAnim != null && handAnim.GetBool("isActive"))
                {
                    isChased = true;
                    yield return StartCoroutine(ChaseCharacter());
                    if (leaveSound != null) audioSource.PlayOneShot(leaveSound);
                }

                yield return null;
            }

            if (!isChased)
            {
                // Игрок не успел прогнать — снимаем батарею
                if (batterySystem != null)
                    batterySystem.DrainBattery(batteryDrainAmount);

                if (noleaveSound != null) audioSource.PlayOneShot(noleaveSound);
                HideCharacter();
            }

            yield return new WaitForSeconds(Random.Range(minReturnTime, maxReturnTime));
        }
    }

    private IEnumerator ChaseCharacter()
    {
        if (characterAnimator != null) characterAnimator.SetBool("isChasing", true);
        yield return new WaitForSeconds(Random.Range(minChaseTime, maxChaseTime));
        HideCharacter();
    }

    private void HideCharacter()
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetBool("isActive", false);
            characterAnimator.SetBool("isChasing", false);
        }
        // Прячем персонажа далеко под сцену пока нет SetActive в аниматоре
        character.transform.position = new Vector3(0f, -1000f, 0f);
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (points == null) return;

        Gizmos.color = new Color(1f, 0.5f, 0f);
        foreach (GameObject p in points)
        {
            if (p == null) continue;
            Gizmos.DrawWireSphere(p.transform.position + Vector3.up * characterHeight, 0.3f);
        }
    }
}
