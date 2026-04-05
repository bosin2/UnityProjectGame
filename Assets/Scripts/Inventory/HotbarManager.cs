using UnityEngine;
using UnityEngine.UI;

public class HotbarManager : MonoBehaviour
{
    public RectTransform highlight;
    public RectTransform[] slots;
    private int currentIdx = -1; // -1로 설정해서 처음엔 아무것도 선택 안 된 상태로 둠

    void Start()
    {
        // 시작할 때는 하이라이트를 비활성화(숨김)
        highlight.gameObject.SetActive(false);
    }

    void Update()
    {
        for (int i = 0; i < 5; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectSlot(i);
            }
        }
    }

    void SelectSlot(int index)
    {
        // 숫자를 누르는 순간 하이라이트를 활성화(보여줌)
        if (!highlight.gameObject.activeSelf)
        {
            highlight.gameObject.SetActive(true);
        }

        currentIdx = index;
        highlight.position = slots[index].position;

        Debug.Log(index + 1 + "번 슬롯 선택됨!");
    }
}