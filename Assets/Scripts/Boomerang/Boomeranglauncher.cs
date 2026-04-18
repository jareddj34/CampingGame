using UnityEngine;

public class BoomerangLauncher : MonoBehaviour
{
    [Header("References")]
    public GameObject boomerangPrefab;     // Drag your boomerang prefab here
    public Transform throwPoint;           // Empty GameObject at camera/hand position

    public PlayerInventory playerInventory;   // Reference to player's inventory to check if holding the boomerang item
    public BoomerangItem boomerangItem;

    [Header("Settings")]
    public KeyCode throwKey = KeyCode.Mouse0;  // Left click to throw

    private GameObject activeBoomerang;
    private bool inFlight = false;

    void Update()
    {
        if (Input.GetKeyDown(throwKey) && !inFlight && playerInventory.IsHoldingItem(playerInventory.itemInHand) && playerInventory.itemInHand.itemName == "Boomerang")
        {
            ThrowBoomerang();
        }
    }

    void ThrowBoomerang()
    {
        if (boomerangPrefab == null || throwPoint == null)
        {
            Debug.LogWarning("BoomerangLauncher: Missing boomerangPrefab or throwPoint reference.");
            return;
        }

        boomerangItem?.HideMesh();   // ← HIDE on throw

        // Spawn at the throw point (typically camera forward for FPS)
        activeBoomerang = Instantiate(boomerangPrefab, throwPoint.position, throwPoint.rotation);

        Boomerang boomerang = activeBoomerang.GetComponent<Boomerang>();
        if (boomerang != null)
        {
            // Use camera forward so it throws where you're looking
            boomerang.Launch(this.transform, throwPoint, Camera.main.transform.forward);
        }

        inFlight = true;
    }

    // Called by the Boomerang when it reaches the player
    public void OnBoomerangCaught()
    {
        boomerangItem?.ShowMesh();   // ← SHOW on catch

        inFlight = false;
        activeBoomerang = null;
        Debug.Log("Boomerang caught!");
    }
}