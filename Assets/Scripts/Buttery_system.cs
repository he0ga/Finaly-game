using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BatterySystem : MonoBehaviour
{
    [Header("Battery UI Settings")]
    [SerializeField] private Texture[] batterySteps;
    [SerializeField] private RawImage batteryUI;

    [Header("Battery Parameters")]
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float currentBattery;

    [Header("Energy Consumption Rates")]
    [SerializeField] private float flashlightConsumption = 5f;
    [SerializeField] private float doorConsumption = 3f;

    [Header("Consuming Objects")]
    [SerializeField] private Light flashlight; // ��������� ����� intensity
    [SerializeField] private Button flashlightButton;
    [SerializeField] private GameObject[] doors; // ��������� ����� activeSelf

    [Header("Controllers")]
    [SerializeField] private GameObject flashlightController; // ������ ��������
    [SerializeField] private GameObject doorController; // ������ ���������� �������

    [SerializeField] private AudioSource[] DoorSounds;
    [SerializeField] private GameObject RemoteControll;

    [Header("Time Settings")]
    [SerializeField] private Text timeText;
    [SerializeField] private float timePerHour = 90f;

    [SerializeField] private string Scene;
    [SerializeField] private string NightRestart;

    [SerializeField] private GameObject Lighting;

    private float timer = 0f;
    private bool nightOver = false;
    private int currentHour = 0;

    void Start()
    {
        currentBattery = maxBattery;
        if (doorController != null) doorController.SetActive(true);
        UpdateTimeDisplay();
    }

    void Update()
    {
        // ������������ ����� �����������
        float totalConsumption = CalculateTotalConsumption();

        // ��������� �������, ���� ���� �������� �����������
        if (totalConsumption > 0f && currentBattery > 0f)
        {
            currentBattery -= totalConsumption * Time.deltaTime;
            currentBattery = Mathf.Clamp(currentBattery, 0f, maxBattery);
            UpdateBatteryUI();
        }

        // �������������� ���������� ��� ��������
        if (currentBattery <= 0f)
        {
            HandleBatteryDepletion();
        }

        if (nightOver) return;

        timer += Time.deltaTime;

        if (timer >= timePerHour)
        {
            timer = 0f;
            currentHour++;

            if (currentHour > 6)
            {
                nightOver = true;
                timeText.text = "6 AM"; // ����� �������� �������� ������
                SceneManager.LoadScene(Scene);
                Debug.Log("���� ���������!");
            }
            else
            {
                UpdateTimeDisplay();
            }
        }
    }

    float CalculateTotalConsumption()
    {
        float consumption = 0f;

        // ����������� �������� (��������� ����� intensity)
        if (flashlight != null && flashlight.intensity > 0)
        {
            consumption += flashlightConsumption;
        }

        // ����������� ������ (��������� ����� activeSelf)
        foreach (var door in doors)
        {
            if (door != null && door.activeSelf)
            {
                consumption += doorConsumption;
            }
        }

        return consumption;
    }

    void HandleBatteryDepletion()
    {
        // ��������� �������
        if (flashlight != null && flashlight.intensity > 0)
        {
            ToggleFlashlight();
        }

        // ��������� ��� �����
        foreach (var door in doors)
       {
            foreach (var sounds in DoorSounds)
            {
                if (door != null && door.activeSelf)
                {
                    door.SetActive(false);
                    sounds.Play();
                    RemoteControll.SetActive(false);
                }
            }
        }

        Lighting.SetActive(false);

        // ��������� �����������
        if (doorController != null) doorController.SetActive(false);
        if (flashlightController != null) flashlightController.SetActive(false);

        
    }

    void ToggleFlashlight()
    {
        if (flashlight == null) return;

        bool turnOn = flashlight.intensity <= 0;

        // �������� ������ ���� ���� ����� ��� ����� ���������
        if (currentBattery > 0f || !turnOn)
        {
            flashlight.intensity = turnOn ? 1f : 0f;
            if (flashlightController != null)
                flashlightController.SetActive(turnOn);
        }
    }

    void UpdateBatteryUI()
    {
        if (batterySteps.Length == 0 || batteryUI == null) return;

        float percent = currentBattery / maxBattery;
        int step = Mathf.Clamp(Mathf.FloorToInt(percent * (batterySteps.Length - 1)), 0, batterySteps.Length - 1);
        batteryUI.texture = batterySteps[step];
    }

    void UpdateTimeDisplay()
    {
        int displayHour = currentHour == 0 ? 12 : currentHour;
        timeText.text = displayHour + " AM";
    }

    public void DrainBattery(float amount)
    {
        currentBattery -= amount;
        currentBattery = Mathf.Clamp(currentBattery, 0f, maxBattery);
        UpdateBatteryUI();

        if (currentBattery <= 0f)
        {
            HandleBatteryDepletion();
        }
    }

    public void restartGame()
    {
        SceneManager.LoadScene(NightRestart);
    }

    public void returnMenu()
    {
        PlayerPrefs.SetInt("ReturnedFromGame", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Menu");
    }
}