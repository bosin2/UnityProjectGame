using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

// 튜토리얼 이벤트 흐름을 제어하는 컴포넌트 (시계 확인 -> NPC 대화 -> 게임 씬 전환)
public class TutorialManager : MonoBehaviour
{
    public Image fadePanel; // 씬 전환 시 페이드 아웃에 사용할 검정 패널

    // Interactable 이벤트: 시계를 확인했을 때 호출
    public void OnClockDone()
    {
        GameManager.Instance.SetFlag("clockChecked");
    }

    // Interactable 이벤트: NPC 대화 완료 후 게임 씬으로 전환
    public void OnNPCDone()
    {
        GameManager.Instance.stage = 1;
        StartCoroutine(FadeAndLoad("GameLap"));
    }

    // 화면을 검정으로 페이드 아웃 후 지정 씬 로드
    IEnumerator FadeAndLoad(string sceneName)
    {
        fadePanel.gameObject.SetActive(true);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime;
            fadePanel.color = new Color(0, 0, 0, t);
            yield return null;
        }
        SceneManager.LoadScene(sceneName);
    }
}
