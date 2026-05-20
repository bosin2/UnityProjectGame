using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 카테고리 탭 버튼.
/// GetComponentInParent 로 같은 계층의 InventoryManager를 찾아 클릭 이벤트를 전달한다.
/// Inspector에서 categoryIndex를 0(열쇠) / 1(소비) / 2(장비)로 설정.
/// </summary>
public class CategoryButton : MonoBehaviour
{
    public int categoryIndex; // 0=열쇠, 1=소비, 2=장비

    void Start()
    {
        // 같은 계층에서 InventoryManager 탐색 (부모 방향)
        InventoryManager inv = GetComponentInParent<InventoryManager>();

        // 없으면 씬 전체에서 탐색
        if (inv == null)
            inv = FindFirstObjectByType<InventoryManager>();

        if (inv == null)
        {
            Debug.LogWarning("[CategoryButton] InventoryManager를 찾을 수 없습니다.", this);
            return;
        }

        GetComponent<Button>().onClick.AddListener(() =>
            inv.OnClickCategory(categoryIndex));
    }
}
