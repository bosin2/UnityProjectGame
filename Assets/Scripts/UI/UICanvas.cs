using UnityEngine;

// 씬 전환 후에도 유지되는 HUD(타이머, HP바) 표시/숨김을 관리하는 싱글톤
public class UICanvas : MonoBehaviour
{
    public static UICanvas Instance;

    [Header("HUD 오브젝트")]
    public GameObject timerUI;
    public GameObject hpUI;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    // 게임 플레이 씬에서 HUD 전체 표시
    public void ShowUI()
    {
        if (timerUI != null) timerUI.SetActive(true);
        if (hpUI != null) hpUI.SetActive(true);
    }

    public void HideUI()
    {
        if (timerUI != null) timerUI.SetActive(false);
        if (hpUI != null) hpUI.SetActive(false);
    }
}
