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
        timerUI.SetActive(true);
        hpUI.SetActive(true);
    }

    // 메뉴/인트로/튜토리얼에서 HUD 전체 숨김
    public void HideUI()
    {
        timerUI.SetActive(false);
        hpUI.SetActive(false);
    }
}
