using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory localInventory;

    public Transform itemPoint;
    public Item itemInHand;


    public void EquipItem(Item item)
    {
        if(!item)
        {
            return;
        }

        itemInHand = Instantiate(item, itemPoint.position, Quaternion.identity, itemPoint);
        itemInHand.transform.localRotation = Quaternion.identity;
        itemInHand.GetComponentInChildren<Collider>().enabled = false;
        itemInHand.GetComponent<Rigidbody>().isKinematic = true;

        // If it's a boomerang, tell the launcher about the new clone
        BoomerangItem boomerangItem = itemInHand.GetComponent<BoomerangItem>();
        if (boomerangItem != null)
        {
            GetComponent<BoomerangLauncher>().boomerangItem = boomerangItem;
        }
    }

    public void UnequipItem(Item item)
    {
        if(!item)
        {
            return;
        }

        if(!itemInHand)
        {
            return;
        }

        if(itemInHand.itemName != item.itemName)
        {
            return;
        }

        // Clear the boomerang reference if we're unequipping it
        if (itemInHand is BoomerangItem)
        {
            GetComponent<BoomerangLauncher>().boomerangItem = null;
        }

        Destroy(itemInHand.gameObject);
        itemInHand = null;

    }

    public bool IsHoldingItem(Item item)
    {
        if(!itemInHand)
        {
            return false;
        }

        return item == itemInHand;
    }

    public void UseItemInHand()
    {
        if(!itemInHand)
        {
            return;
        }

        itemInHand.UseItem();
    }

    // Called on input RELEASE — only charge-based items (e.g. BowItem) respond to this
    public void ReleaseItemInHand()
    {
        if (!itemInHand)
            return;
 
        if (itemInHand is Bow bow)
        {
            bow.ReleaseItem();
        }
    }
}
