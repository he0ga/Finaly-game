using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class ExitToMainMenu : MonoBehaviour
{
    public Text exitMessageText;  // ������ �� UI Text ��� ���������
    public string mainMenuSceneName = "MainMenu";  // ��� ����� �������� ����
    public float holdTimeToExit = 2f;  // ����� ��������� ESC ��� ������

    private bool isEscHeld = false;
    private float escHoldTimer = 0f;

    void Start()
    {
        if (exitMessageText != null)
            exitMessageText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isEscHeld = true;
            escHoldTimer = 0f;
            if (exitMessageText != null)
                exitMessageText.gameObject.SetActive(true);
            exitMessageText.text = "Зажмите чтобы выйти";
        }

        if (Input.GetKeyUp(KeyCode.Escape))
        {
            isEscHeld = false;
            escHoldTimer = 0f;
            if (exitMessageText != null)
                exitMessageText.gameObject.SetActive(false);
        }

        if (isEscHeld)
        {
            escHoldTimer += Time.deltaTime;
            if (escHoldTimer >= holdTimeToExit)
            {
                SaveCurrentScene();
                SceneManager.LoadScene(mainMenuSceneName);
                Cursor.lockState = CursorLockMode.None;  // ������������ �������
                Cursor.visible = true;                   // ������� ������ �������
            }
        }
    }

    void SaveCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString("LastScene", currentSceneName);
        PlayerPrefs.SetInt("ReturnedFromGame", 1);
        PlayerPrefs.Save();
        Debug.Log("Сцена сохранена: " + currentSceneName);
    }


}
