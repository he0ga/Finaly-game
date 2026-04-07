using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHoverAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float scaleFactor = 1.1f;
    public float animationDuration = 0.2f;
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 originalScale;
    private Coroutine hoverCoroutine;

    private void Start()
    {
        originalScale = transform.localScale;

        // Создаем плавную кривую по умолчанию (easeOutBack для более динамичного эффекта)
        if (easeCurve.length == 2) // Если кривая не была настроена в инспекторе
        {
            easeCurve = new AnimationCurve(
                new Keyframe(0, 0, 0, 0),
                new Keyframe(0.5f, 1.1f, 0, 0),
                new Keyframe(1, 1, 0, 0)
            );
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
        }
        hoverCoroutine = StartCoroutine(AnimateHover(true));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
        }
        hoverCoroutine = StartCoroutine(AnimateHover(false));
    }

    private IEnumerator AnimateHover(bool isHovering)
    {
        float time = 0;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = isHovering ? originalScale * scaleFactor : originalScale;

        while (time < animationDuration)
        {
            time += Time.deltaTime;
            float t = time / animationDuration;

            // Применяем easing функцию для плавности
            float easedT = ApplyEasing(t, isHovering);

            transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, easedT);
            yield return null;
        }

        transform.localScale = targetScale;
    }

    private float ApplyEasing(float t, bool isHovering)
    {
        // Разные easing функции для наведения и ухода
        if (isHovering)
        {
            // EaseOutBack для наведения - с небольшим overshoot
            return EaseOutBack(t);
        }
        else
        {
            // EaseInOutBack для ухода - плавное возвращение
            return EaseInOutBack(t);
        }
    }

    // Easing функции для плавных анимаций
    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private float EaseInOutBack(float t)
    {
        float c1 = 1.70158f;
        float c2 = c1 * 1.525f;

        return t < 0.5f
            ? (Mathf.Pow(2f * t, 2f) * ((c2 + 1f) * 2f * t - c2)) / 2f
            : (Mathf.Pow(2f * t - 2f, 2f) * ((c2 + 1f) * (t * 2f - 2f) + c2) + 2f) / 2f;
    }

    private float EaseOutElastic(float t)
    {
        float c4 = (2f * Mathf.PI) / 3f;
        return t == 0f ? 0f : t == 1f ? 1f : Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
    }

    private float EaseInOutQuint(float t)
    {
        return t < 0.5f ? 16f * t * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 5f) / 2f;
    }

    // Анимация клика с улучшенной плавностью
    public void AnimateClick()
    {
        StartCoroutine(ClickAnimation());
    }

    private IEnumerator ClickAnimation()
    {
        Vector3 currentScale = transform.localScale;
        float time = 0;

        // Уменьшение с easing
        while (time < animationDuration / 2f)
        {
            time += Time.deltaTime;
            float t = time / (animationDuration / 2f);
            float easedT = EaseOutBack(t);
            transform.localScale = Vector3.Lerp(currentScale, currentScale * 0.85f, easedT);
            yield return null;
        }

        time = 0;

        // Возврат с easing
        while (time < animationDuration / 2f)
        {
            time += Time.deltaTime;
            float t = time / (animationDuration / 2f);
            float easedT = EaseOutElastic(t);
            transform.localScale = Vector3.Lerp(currentScale * 0.85f, currentScale, easedT);
            yield return null;
        }

        transform.localScale = currentScale;
    }

    // Дополнительные методы для разных типов анимаций
    public void AnimateHoverWithBounce()
    {
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
        }
        hoverCoroutine = StartCoroutine(AnimateHoverWithBounceCoroutine());
    }

    private IEnumerator AnimateHoverWithBounceCoroutine()
    {
        float time = 0;
        Vector3 startScale = transform.localScale;

        while (time < animationDuration)
        {
            time += Time.deltaTime;
            float t = time / animationDuration;
            float scale = 1f + (scaleFactor - 1f) * EaseOutBounce(t);
            transform.localScale = originalScale * scale;
            yield return null;
        }

        transform.localScale = originalScale * scaleFactor;
    }

    private float EaseOutBounce(float t)
    {
        float n1 = 7.5625f;
        float d1 = 2.75f;

        if (t < 1f / d1)
        {
            return n1 * t * t;
        }
        else if (t < 2f / d1)
        {
            return n1 * (t -= 1.5f / d1) * t + 0.75f;
        }
        else if (t < 2.5f / d1)
        {
            return n1 * (t -= 2.25f / d1) * t + 0.9375f;
        }
        else
        {
            return n1 * (t -= 2.625f / d1) * t + 0.984375f;
        }
    }
}