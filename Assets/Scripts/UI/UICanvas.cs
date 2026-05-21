using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// HUD(타이머, HP바) 표시/숨김 관리.
/// 씬 전환 후에도 유지되며 GameManager가 씬 이름에 따라 ShowUI/HideUI를 호출한다.
/// </summary>
public class UICanvas : MonoBehaviour
{
    // 소프트 참조 (강제 싱글톤 아님)
    public static UICanvas Instance { get; private set; }

    [Header("HUD 오브젝트")]
    public GameObject timerUI;
    public GameObject hpUI;

    void Awake()
    {
        // 중복 방지 — CameraFollow, PlayerMovement와 동일한 패턴
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ShowUI()
    {
        if (timerUI != null) timerUI.SetActive(true);
        if (hpUI    != null) hpUI.SetActive(true);
    }

    public void HideUI()
    {
        if (timerUI != null) timerUI.SetActive(false);
        if (hpUI    != null) hpUI.SetActive(false);
    }
}
