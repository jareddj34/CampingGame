using UnityEngine;

public class Hitbox : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if(ph != null)
        {
            ph.TakeDamage(1);
        }

        PlayerKnockback pk = other.GetComponent<PlayerKnockback>();
        if(pk != null)
        {
            Vector3 knockbackDir = (other.transform.position - transform.position).normalized;
            knockbackDir.y = 0f; // flatten it so it's purely horizontal
            knockbackDir = (knockbackDir + Vector3.up * 0.4f).normalized; // add a small upward kick
            pk.ApplyKnockback(knockbackDir * 6f); // 6f is the force — tune this in Play mode
        }
    }
}
