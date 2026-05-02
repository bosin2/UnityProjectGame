using UnityEngine;
using System.Collections;

// 씬 시작 시 키 안내 UI를 페이드 인/아웃으로 보여주고 자동으로 사라지는 컴포넌트
public class KeyGuide : MonoBehaviour
{
    public CanvasGroup guideGroup;
    public float showDuration = 2f;   // 완전히 표시된 채 유지할 시간(초)
    public float fadeDuration = 0.5f; // 페이드 인/아웃 속도(초)

    void Start()
    {
        StartCoroutine(ShowAndFade());
    }

    // 페이드 인 -> 대기 -> 페이드 아웃 순서로 안내 UI 표시
    IEnumerator ShowAndFade()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            guideGroup.alpha = t;
            yield return null;
        }

        yield return new WaitForSeconds(showDuration);

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
