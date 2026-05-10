using UnityEngine;

// 아이템 하나의 기본 정보와 효과 수치를 담는 ScriptableObject.
// 인벤토리, 핫바, ItemGiver 등에서 참조한다.
[CreateAssetMenu(fileName = "NewItem", menuName = "the4f/Item")]
public class ItemData : ScriptableObject
{
    [Header("기본 정보")]
    public string itemName;
    public string description;
    public Sprite icon;
    public ItemType type;

    [Header("효과 수치")]
    public int healAmount;
    public float defenseAmount;
    public float speedAmount;
    public float speedDuration;
    public string keyId;           
}

public enum ItemType
{
    Heal,
    Key,
    Armor,
    Shoes,
    SpeedBoost
}

// ItemStack.cs 에서 이관 — 아이템과 수량을 함께 저장하는 데이터 클래스
[System.Serializable]
public class ItemStack
{
    public ItemData item;
    public int count;

    public ItemStack(ItemData item, int count)
    {
        this.item = item;
        this.count = count;
    }
}