using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public GameObject inventoryUI; //Inventory_UI를 넣을 칸
    public GameObject hotbarUI;

    private bool isInventoryOpen = false;

    void Start()
    {
        // 게임 시작할 때는 인벤토리를 꺼둠
        isInventoryOpen = false;
        inventoryUI.SetActive(false);
        hotbarUI.SetActive(true);
    }

    void Update()
    {
        // q 키를 누르면 상태 반전
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ToggleInventory();

        }
    }

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        inventoryUI.SetActive(isInventoryOpen);
        hotbarUI.SetActive(!isInventoryOpen);

        // 인벤토리가 열려 있을 때는 시간이 멈추게
    }
}