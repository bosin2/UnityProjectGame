using UnityEngine;
using UnityEngine.UI;

// 인벤토리 카테고리 탭 버튼. categoryIndex를 설정해 두면
// 클릭 시 InventoryManager.OnClickCategory(categoryIndex)를 자동 호출한다.
public class CategoryButton : MonoBehaviour
{
    public int categoryIndex; // 0=열쇠, 1=소비, 2=장비

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            InventoryManager.Instance.OnClickCategory(categoryIndex);
        });
    }
}