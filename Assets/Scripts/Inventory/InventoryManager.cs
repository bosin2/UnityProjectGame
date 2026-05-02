using UnityEngine;

// E키로 인벤토리를 열고 닫으며, 열려 있을 때 핫바를 숨기는 컴포넌트
public class InventoryManager : MonoBehaviour
{
    public GameObject inventoryUI;
    public GameObject hotbarUI;

    private bool isInventoryOpen = false;

    void Start()
    {
        // 게임 시작 시 인벤토리 닫힘 상태로 초기화
        isInventoryOpen = false;
        inventoryUI.SetActive(false);
        hotbarUI.SetActive(true);
    }

    void Update()
    {
        // E키로 인벤토리 열기/닫기 전환
        if (Input.GetKeyDown(KeyCode.E))
            ToggleInventory();
    }

    // 인벤토리와 핫바 표시 상태를 반전
    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        inventoryUI.SetActive(isInventoryOpen);
        hotbarUI.SetActive(!isInventoryOpen);
    }
}
