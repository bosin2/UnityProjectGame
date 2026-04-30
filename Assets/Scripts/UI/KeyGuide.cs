using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class KeyGuide : MonoBehaviour
{
    public CanvasGroup guideGroup;
    public float showDuration = 2f; // 몇 초 보여줄지
    public float fadeDuration = 0.5f; // 페이드 속도

    void Start()
    {
        StartCoroutine(ShowAndFade());
    }

    IEnumerator ShowAndFade()
    {
        // 페이드인
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            guideGroup.alpha = t;
            yield return null;
        }

        // 잠깐 유지
        yield return new WaitForSeconds(showDuration);

        // 페이드아웃
        t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime / fadeDuration;
            guideGroup.alpha = t;
            yield return null;
        }

        gameObject.SetActive(false);
    }
}