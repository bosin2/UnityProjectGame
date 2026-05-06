using UnityEngine;
using UnityEngine.UI;

public class CategoryButton : MonoBehaviour
{
    public int categoryIndex;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            InventoryManager.Instance.OnClickCategory(categoryIndex);
        });
    }
}