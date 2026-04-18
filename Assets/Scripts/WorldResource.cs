using UnityEngine;
using System.Collections;

public class WorldResource :MonoBehaviour, IDamageable
{

    public int resourceHealth;

    [Header("Drop Settings")]
    public GameObject[] dropItems; // Array of items to drop when resource is depleted
    public int dropAmount = 3; // Number of items to drop

    public float popDuration = 0.3f;
    public float popIntensity = 0.2f;
    public AnimationCurve popCurve;

    public void TakeDamage(int damage)
    {

        StartCoroutine(OnPop());
        resourceHealth -= damage;

        if (resourceHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator OnPop()
    {
        float timer = 0f;
        Vector3 originalScale = transform.localScale;

        while (timer < popDuration)
        {
            timer += Time.deltaTime;
            float scaleMultiplier = 1 + popCurve.Evaluate(timer / popDuration) * popIntensity;
            transform.localScale = originalScale * scaleMultiplier;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    public void Die()
    {
        // Handle resource depletion logic here (e.g., play animation, drop items, etc.)
        for (int i = 0; i < dropAmount; i++)
        {
            Instantiate(dropItems[Random.Range(0, dropItems.Length)], transform.position + Vector3.up, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}
