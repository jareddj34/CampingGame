using UnityEngine;

public class Resource : Item
{
    public override void UseItem()
    {
        base.UseItem();

        Debug.Log($"Using {itemName}");
    }
}
