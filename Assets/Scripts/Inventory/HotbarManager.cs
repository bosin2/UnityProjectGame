using UnityEngine;
using UnityEngine.UI;

// 숫자키(1~5)로 핫바 슬롯을 선택하고 하이라이트를 이동시키는 컴포넌트
public class HotbarManager : MonoBehaviour
{
    public RectTransform highlight;
    public RectTransform[] slots;
    private int currentIdx = -1; // -1: 아무 슬롯도 선택되지 않은 초기 상태

    void Start()
    {
        // 시작 시 하이라이트 비활성화
        highlight.gameObject.SetActive(false);
    }

    void Update()
    {
        // 숫자키 1~5에 해당하는 슬롯 선택
        for (int i = 0; i < 5; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                SelectSlot(i);
        }
    }

    // 지정 슬롯에 하이라이트를 이동하고 현재 선택 인덱스 갱신
    void SelectSlot(int index)
    {
        if (!highlight.gameObject.activeSelf)
            highlight.gameObject.SetActive(true);

        currentIdx = index;
        highlight.position = slots[index].position;
    }
}
