using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor.Build.Content;

public class HoverAction : MonoBehaviour
{
    [Header("UI элемент для надписи")]
    public Text hoverText;  // перетащите сюда UI Text из Canvas
    public string message = "Нажмите E для действия";
    [TextArea(5, 10)]
    public string dialogueText;

    [Header("Настройка")]
    public KeyCode actionKey = KeyCode.E;
    public string Scene;
    public bool SceneLoad;
    public bool dialogueBool;


    [Header("Настройки взаимодействия")]
    public Transform playerTransform; // Ссылка на позицию игрока или камеры
    public float interactDistance = 3f; // Максимальная дистанция взаимодействия

    private bool isHovering = false;

    

    void Start()
    {
        if (hoverText != null)
            hoverText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (isHovering)
        {
            if (SceneLoad)
            {
                if (Input.GetKeyDown(actionKey))
                {
                    if (playerTransform != null)
                    {
                        float dist = Vector3.Distance(playerTransform.position, transform.position);
                        if (dist <= interactDistance)
                        {
                            PerformAction();
                            if (!string.IsNullOrEmpty(Scene))
                                SceneManager.LoadScene(Scene);
                        }
                        else
                        {
                            Debug.Log("Слишком далеко для взаимодействия");
                            
                        }
                    }
                    else
                    {
                        PerformAction();
                    }
                }
            }
            if (dialogueBool)
            {
                if (Input.GetKeyDown(actionKey))
                {
                    if (playerTransform != null)
                    {
                        float dist = Vector3.Distance(playerTransform.position, transform.position);
                        if (dist <= interactDistance)
                        {
                            FindObjectOfType<DialogueTyper>().StartDialogue(dialogueText);

                        }
                        else
                        {
                            Debug.Log("Слишком далеко для взаимодействия");
                        }
                    }

                }
            }
        }
    }

    void OnMouseEnter()
    {
        isHovering = true;
        float dist = Vector3.Distance(playerTransform.position, transform.position);
        if (dist <= interactDistance)
        {
            if (hoverText != null)
            {
                hoverText.text = message;
                hoverText.gameObject.SetActive(true);
            }
        }
        else
        {
            Debug.Log("Слишком далеко для взаимодействия");
            hoverText.gameObject.SetActive(false);
        }
    }

    void OnMouseExit()
    {
        isHovering = false;
        if (hoverText != null)
            hoverText.gameObject.SetActive(false);
    }

    void PerformAction()
    {
        Debug.Log("Действие выполнено при нажатии на кнопку.");
        
    }

}
