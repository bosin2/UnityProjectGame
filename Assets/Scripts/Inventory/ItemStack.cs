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