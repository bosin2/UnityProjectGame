using UnityEngine;

// 핫바 UI 오브젝트의 표시/숨김을 외부에서 제어할 수 있는 싱글톤 래퍼
public class HotbarUI : MonoBehaviour
{
    public static HotbarUI Instance;

    void Awake()
    {
        Instance = this;
    }

    // 핫바 표시
    public void Show() => gameObject.SetActive(true);

    // 핫바 숨김
    public void Hide() => gameObject.SetActive(false);
}
