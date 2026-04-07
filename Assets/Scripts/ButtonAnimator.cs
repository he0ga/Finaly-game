using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float scaleFactor = 1.1f; // Фактор увеличения
    public float animationDuration = 0.2f; // Длительность анимации

    private Vector3 originalScale;
    private bool isHovered = false;

    private void Start()
    {
        originalScale = transform.localScale; // Сохраняем оригинальный размер
    }

    private void Update()
    {
        if (isHovered)
        {
            // Увеличиваем размер кнопки
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale * scaleFactor, Time.deltaTime / animationDuration);
        }
        else
        {
            // Возвращаем размер кнопки к оригинальному
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime / animationDuration);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true; // Устанавливаем флаг наведения
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false; // Сбрасываем флаг наведения
    }
}