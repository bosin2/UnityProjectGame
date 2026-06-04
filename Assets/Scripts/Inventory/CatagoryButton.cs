/*
 * CatagoryButton.cs
 * 역할: 인벤토리 카테고리 버튼 클릭을 InventoryManager에 전달하는 UI 연결 스크립트입니다.
 * 연결: Unity UI Button 이벤트에서 호출되어 키/소비/장비 카테고리 전환을 수행합니다.
 * 주의: 파일명은 Catagory로 되어 있지만 코드 참조와 메타 파일이 연결되어 있으므로 이름 변경은 신중해야 합니다.
 */using UnityEngine;
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

