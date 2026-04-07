using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

public class baloonOlegSystem : MonoBehaviour
{
    [Header("Точки для появления персонажа")]
    [SerializeField] private GameObject[] points;
    [Header("Персонаж, который будет появляться")]
    [SerializeField] private GameObject character;
    [Header("Высота персонажа над точкой")]
    [SerializeField] private float characterHeight = 1f;
    [Header("Камера для поворота")]
    [SerializeField] private Camera camera;
    [Header("Настройки поворота")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private bool freezeXRotation = true;
    [SerializeField] private bool freezeZRotation = true;
    [Header("Время стояния на точке")]
    [SerializeField] private float pointWaitTimeMax = 2f;
    [SerializeField] private float pointWaitTimeMin = 0.5f;
    [Header("Время до прогона персонажа")]
    [SerializeField] private float minChaseTime = 1f;
    [SerializeField] private float maxChaseTime = 3f;
    [Header("Animator для рук игрока")]
    [SerializeField] private GameObject handAnimator;
    [Header("Время до возвращения персонажа")]
    [SerializeField] private float minReturnTime = 2f;
    [SerializeField] private float maxReturnTime = 4f;
    [Header("Время до первого появления")]
    [SerializeField] private float minFirstAppearanceTime = 5f;
    [SerializeField] private float maxFirstAppearanceTime = 15f;

    [Header("Время ожидания между стадиями звуков")]
    [SerializeField] private float minStageWait = 0.5f;
    [SerializeField] private float maxStageWait = 1.5f;

    [Header("Звуки стадий перед появлением персонажа")]
    [SerializeField] private AudioClip stage1Sound;
    [SerializeField] private AudioClip stage2Sound;
    [SerializeField] private AudioClip stage3Sound;
    [SerializeField] private AudioClip leaveSound;
    [SerializeField] private AudioClip noleaveSound;

    [Header("Настройки AudioSource")]
    [SerializeField] private float volume = 1f;
    [SerializeField] private bool loop = false;
    [SerializeField] private float pitch = 1f;
    [SerializeField] private int priority = 128;
    [SerializeField] private bool mute = false;

    [Header("Интеграция с BatterySystem")]
    [SerializeField] private BatterySystem batterySystem;
    [SerializeField] private float batteryDrainAmount = 10f; // Можно настроить в инспекторе

    private AudioSource audioSource;
    private Animator characterAnimator;
    private Animator handAnim;

    private void Start()
    {
        characterAnimator = character.GetComponent<Animator>();
        if (handAnimator != null)
        {
            handAnim = handAnimator.GetComponent<Animator>();
        }
        else
        {
            Debug.LogError("Hand Animator не назначен!");
        }
        audioSource = gameObject.AddComponent<AudioSource>();

        // Применяем настройки к AudioSource
        audioSource.volume = volume;
        audioSource.loop = loop;
        audioSource.pitch = pitch;
        audioSource.priority = priority;
        audioSource.mute = mute;

        // Скрываем персонажа в начале игры
        HideCharacter();
        // Запускаем корутину с задержкой перед первым появлением
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        // Ждем случайное время перед первым появлением
        yield return new WaitForSeconds(Random.Range(minFirstAppearanceTime, maxFirstAppearanceTime));
        // Запускаем основной цикл поведения
        StartCoroutine(CharacterRoutine());
    }

    private IEnumerator CharacterRoutine()
    {
        while (true)
        {
            // Первая стадия - проиграть первый звук
            if (stage1Sound != null)
            {
                audioSource.PlayOneShot(stage1Sound);
                yield return new WaitForSeconds(stage1Sound.length);
            }
            // Ожидание между первой и второй стадией
            yield return new WaitForSeconds(Random.Range(minStageWait, maxStageWait));

            // Вторая стадия - проиграть второй звук
            if (stage2Sound != null)
            {
                audioSource.PlayOneShot(stage2Sound);
                yield return new WaitForSeconds(stage2Sound.length);
            }
            // Ожидание между второй и третьей стадией
            yield return new WaitForSeconds(Random.Range(minStageWait, maxStageWait));

            // Третья стадия - проиграть третий звук
            if (stage3Sound != null)
            {
                audioSource.PlayOneShot(stage3Sound);
                yield return new WaitForSeconds(stage3Sound.length);
            }

            // Появление персонажа
            GameObject randomPoint = points[Random.Range(0, points.Length)];
            character.transform.position = randomPoint.transform.position + Vector3.up * characterHeight;
            if (characterAnimator != null) characterAnimator.SetBool("isActive", true);

            // Ожидание с проверкой isActive
            float waitTime = Random.Range(pointWaitTimeMin, pointWaitTimeMax);
            float timer = 0f;
            bool isChased = false;
            while (timer < waitTime && !isChased)
            {
                timer += Time.deltaTime;
                // Поворот к камере
                Vector3 dirToCam = camera.transform.position - character.transform.position;
                Quaternion lookRot = Quaternion.LookRotation(dirToCam);
                Vector3 euler = lookRot.eulerAngles;
                if (freezeXRotation) euler.x = 0;
                if (freezeZRotation) euler.z = 0;
                character.transform.rotation = Quaternion.Slerp(character.transform.rotation, Quaternion.Euler(euler), rotationSpeed * Time.deltaTime);

                if (handAnim != null && handAnim.GetBool("isActive"))
                {
                    isChased = true;
                    yield return StartCoroutine(ChaseCharacter());
                    audioSource.PlayOneShot(leaveSound);
                }
                yield return null;
            }
            if (!isChased)
            {
                // Снимаем заряд с батареи
                if (batterySystem != null)
                {
                    batterySystem.DrainBattery(batteryDrainAmount);
                    audioSource.PlayOneShot(noleaveSound);
                }
                HideCharacter();
            }

            // Пауза перед следующим появлением с использованием minReturnTime и maxReturnTime
            yield return new WaitForSeconds(Random.Range(minReturnTime, maxReturnTime));
        }
    }

    private IEnumerator ChaseCharacter()
    {
        if (characterAnimator != null) characterAnimator.SetBool("isChasing", true);
        float chaseTime = Random.Range(minChaseTime, maxChaseTime);
        yield return new WaitForSeconds(chaseTime);
        HideCharacter();
    }

    private void HideCharacter()
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetBool("isActive", false);
            characterAnimator.SetBool("isChasing", false);
        }
        character.transform.position = new Vector3(0, -1000, 0);
    }
}
