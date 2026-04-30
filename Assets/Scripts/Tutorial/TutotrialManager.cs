using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public Image fadePanel; // 검정 Image 연결

    public void OnClockDone()
    {
        GameManager.instance.SetFlag("clockChecked");
    }

    public void OnNPCDone()
    {
        GameManager.instance.stage = 1;
        StartCoroutine(FadeAndLoad("GameLap"));
    }

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