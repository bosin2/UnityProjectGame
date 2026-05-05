using UnityEngine;

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