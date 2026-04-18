using UnityEngine;

public class Boomerang : MonoBehaviour
{
    [Header("Flight Settings")]
    public float forwardSpeed = 15f;       // Speed when thrown outward
    public float returnSpeed = 18f;        // Speed when flying back
    public float travelDistance = 12f;     // Distance before it turns around
    public float rotationSpeed = 720f;     // Spin speed in degrees/sec

    private Transform player;
    private Transform throwPoint;
    private Vector3 throwDirection;
    private float distanceTravelled = 0f;
    private bool returning = false;
    private bool hasHit = false;

    // Call this right after instantiating the boomerang
    public void Launch(Transform playerTransform, Transform throwPointTransform, Vector3 direction)
    {
        player = playerTransform;
        throwPoint = throwPointTransform;
        throwDirection = direction.normalized;
    }

    void Update()
    {
        if (player == null) return;

        // Spin the boomerang visually
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);

        if (!returning)
        {
            // Fly forward
            transform.position += throwDirection * forwardSpeed * Time.deltaTime;
            distanceTravelled += forwardSpeed * Time.deltaTime;

            if (distanceTravelled >= travelDistance)
                returning = true;
        }
        else
        {
            ReturnToPlayer();
        }
    }

    void ReturnToPlayer()
    {
        // Recalculate direction every frame so it tracks a moving player,
        // but always travels in a straight line toward the current position.
        Vector3 directionToThrowPoint = (throwPoint.position - transform.position).normalized;
        transform.position += directionToThrowPoint * returnSpeed * Time.deltaTime;

        // Catch detection — close enough to throw point
        if (Vector3.Distance(transform.position, throwPoint.position) < 1f)
        {
            // Notify the player controller that it was caught
            player.GetComponent<BoomerangLauncher>().OnBoomerangCaught();
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Ignore the player
        if (other.transform == player) return;
        // Ignore other boomerangs
        if (other.GetComponent<Boomerang>() != null) return;

        if (!hasHit)
        {
            hasHit = true;

            // Optional: deal damage if the object has a health component
            // other.GetComponent<Health>()?.TakeDamage(25f);
            other.GetComponent<IDamageable>()?.TakeDamage(2); // example damage value

            other.GetComponent<MobHealth>()?.TakeDamage(2f, player.transform.position); // example damage value for mobs

            // Start flying back immediately from the hit point
            returning = true;
        }
    }
}