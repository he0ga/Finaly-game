using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class ExitToMainMenu : MonoBehaviour
{
    public Text exitMessageText;  // Ссылка на UI Text для сообщения
    public string mainMenuSceneName = "MainMenu";  // Имя сцены главного меню
    public float holdTimeToExit = 2f;  // Время удержания ESC для выхода

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
                Cursor.lockState = CursorLockMode.None;  // Освобождение курсора
                Cursor.visible = true;                   // Сделать курсор видимым
            }
        }
    }

    void SaveCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString("LastScene", currentSceneName);
        PlayerPrefs.Save();
        Debug.Log("Сцена сохранена: " + currentSceneName);
    }


}
