using UnityEngine;

public class UICanvas : MonoBehaviour
{
    public static UICanvas Instance;

    [Header("UI 오브젝트들")]
    public GameObject timerUI;
    public GameObject hpUI;

    public void ShowUI()
    {
        timerUI.SetActive(true);
        hpUI.SetActive(true);
    }

    public void HideUI()
    {
        timerUI.SetActive(false);
        hpUI.SetActive(false);
    }

    void Awake()
    {
        if (FindObjectsByType<UICanvas>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}