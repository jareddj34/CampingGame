using UnityEngine;
using System.Collections;

public class Bow : Item
{
    [Header("References")]
    public Animator animator;
    public GameObject arrowPrefab;         // Assign your Arrow prefab here
    public Transform arrowSpawnPoint;      // Empty child GameObject at the bow's nock point

    private Transform player;

    [Header("Tuning")]
    public float minShootForce = 10f;
    public float maxShootForce = 40f;
    public float maxChargeTime = 1.5f;     // How long until the bow is "fully drawn"
    public float shootCooldown = 1.2f;     // Must wait this long between shots (covers reload anim)

    // State
    private bool isPulling = false;
    private float pullStartTime;
    private float lastShootTime = -999f;
    private bool isReloading = false;

    private void Start()
    {
        player = transform.root; // assumes the bow is a child of the player object
    }

    // ---------------------------------------------------------------
    // Called by PlayerInventory.UseItemInHand() on left-click PRESS
    // ---------------------------------------------------------------
    public override void UseItem()
    {
        // Block if reloading or on cooldown
        if (isReloading || Time.time - lastShootTime < shootCooldown)
            return;

        // Block if already pulling (shouldn't normally happen, but safety check)
        if (isPulling)
            return;

        isPulling = true;
        pullStartTime = Time.time;
        animator.SetBool("IsPulling", true);
    }

    // ---------------------------------------------------------------
    // Called by InputHandler on left-click RELEASE
    // ---------------------------------------------------------------
    public void ReleaseItem()
    {
        if (!isPulling)
            return;

        isPulling = false;
        animator.SetBool("IsPulling", false);

        float chargeTime = Mathf.Clamp(Time.time - pullStartTime, 0f, maxChargeTime);
        float chargePercent = chargeTime / maxChargeTime;           // 0..1
        float force = Mathf.Lerp(minShootForce, maxShootForce, chargePercent);

        StartCoroutine(ShootArrow(force));
    }

    private IEnumerator ShootArrow(float force)
    {
        isReloading = true;
        lastShootTime = Time.time;

        // Trigger the shoot animation
        animator.SetTrigger("Shoot");

        // Brief wait so the "Shoot" animation can play its release frame
        // before we spawn the arrow (tune this to match your animation)
        yield return new WaitForSeconds(0.08f);

        // Spawn arrow and fire it
        if (arrowPrefab != null && arrowSpawnPoint != null)
        {
            GameObject arrowGO = Instantiate(
                arrowPrefab,
                arrowSpawnPoint.position,
                arrowSpawnPoint.rotation
            );

            Arrow arrow = arrowGO.GetComponent<Arrow>();
            if (arrow != null)
            {
                arrow.Launch(Camera.main.transform.forward, force);
            }
        }

        // Wait for the reload animation to finish before allowing another shot.
        // Set this to match the length of your Reload clip.
        yield return new WaitForSeconds(shootCooldown - 0.08f);

        isReloading = false;
    }
}