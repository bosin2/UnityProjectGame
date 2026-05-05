using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class InventoryManager : MonoBehaviour
{
    [System.Serializable]
    public class InventoryPage
    {
        public Button categoryButton;       // 위쪽 버튼
        public GameObject contentPanel;     // 아래에 보여줄 창
        public GameObject firstContentItem; // 아래 창의 첫 번째 선택 요소
    }

    public GameObject inventoryUI;
    public GameObject hotbarUI;
    public InventoryPage[] pages;

    private bool isInventoryOpen = false;
    private float previousTimeScale = 1f;
    private int currentPageIndex = -1;

    void Start()
    {
        // 게임 시작할 때는 인벤토리를 꺼둠
        isInventoryOpen = false;
        inventoryUI.SetActive(false);
        hotbarUI.SetActive(true);

        for (int i = 0; i < pages.Length; i++)
        {
            int index = i;

            if (pages[i].categoryButton != null)
                pages[i].categoryButton.onClick.AddListener(() => EnterPage(index));

            if (pages[i].contentPanel != null)
                pages[i].contentPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            ToggleInventory();

        if (!isInventoryOpen)
            return;

        UpdatePageBySelectedButton();
    }

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;

        inventoryUI.SetActive(isInventoryOpen);
        hotbarUI.SetActive(!isInventoryOpen);

        if (isInventoryOpen)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            StartCoroutine(SelectFirstCategoryNextFrame());
        }
        else
        {
            Time.timeScale = previousTimeScale;
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    IEnumerator SelectFirstCategoryNextFrame()
    {
        yield return null;

        if (pages.Length == 0 || pages[0].categoryButton == null)
            yield break;

        ShowPage(0);

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(pages[0].categoryButton.gameObject);
    }

    void UpdatePageBySelectedButton()
    {
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null)
            return;

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i].categoryButton == null)
                continue;

            if (selected == pages[i].categoryButton.gameObject)
            {
                ShowPage(i);
                return;
            }
        }
    }

    void ShowPage(int index)
    {
        if (currentPageIndex == index)
            return;

        currentPageIndex = index;

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i].contentPanel != null)
                pages[i].contentPanel.SetActive(i == index);
        }
    }

    void EnterPage(int index)
    {
        ShowPage(index);

        if (pages[index].firstContentItem == null)
            return;

        StartCoroutine(SelectContentFirstItemNextFrame(index));
    }

    IEnumerator SelectContentFirstItemNextFrame(int index)
    {
        yield return null;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(pages[index].firstContentItem);
    }
}