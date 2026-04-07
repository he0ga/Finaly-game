using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DialogueTyper : MonoBehaviour
{
    [Header("UI Text для вывода")]
    public Text dialogueText;

    [Header("Настройки тайминга")]
    public float charDelay = 0.05f;
    public float paragraphDelay = 1.5f;
    public float textHideDelay = 1f;

    private Coroutine typingCoroutine;
    private string[] paragraphs;
    public AudioSource textAudio;

    private void Awake()
    {
        if (dialogueText != null)
            dialogueText.gameObject.SetActive(false);
        
    }

    // Запустите диалог из другого скрипта
    public void StartDialogue(string fullText)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        paragraphs = fullText.Split(new[] { "/b" }, System.StringSplitOptions.RemoveEmptyEntries);
        dialogueText.gameObject.SetActive(true);
        typingCoroutine = StartCoroutine(TypeParagraphs());
    }

    private IEnumerator TypeParagraphs()
    {
        for (int i = 0; i < paragraphs.Length; i++)
        {
            string paragraph = paragraphs[i].Trim();
            dialogueText.text = "";

            // Включаем звук и зацикливаем
            if (textAudio != null)
            {
                textAudio.loop = true;
                textAudio.Play();
                Debug.Log("Typing sound started");
            }
            else
            {
                Debug.LogWarning("AudioSource not assigned");
            }

            foreach (char c in paragraph)
            {
                dialogueText.text += c;
                yield return new WaitForSeconds(charDelay);
            }

            // Отключаем звук
            if (textAudio != null)
            {
                textAudio.Stop();
                textAudio.loop = false;
            }

            if (i < paragraphs.Length - 1)
                yield return new WaitForSeconds(paragraphDelay);
        }

        yield return new WaitForSeconds(textHideDelay);
        dialogueText.gameObject.SetActive(false);
    }

}
