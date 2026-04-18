using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Tool : Item
{

    public Animator animator;

    public int damage = 1;

    public float waitBeforeFirstHit = 0.1f;
    public float waitBetweenHitsTime = 0.3f;
    public float useCooldown = 1f;
    public float hitRadius = 0.5f;
    public List<Vector3> hitPoints = new List<Vector3>();

    private WaitForSeconds waitForFirstHit, waitBetweenHits;
    private float lastUseTime;

    private void Awake()
    {
        waitForFirstHit = new WaitForSeconds(waitBeforeFirstHit);
        waitBetweenHits = new WaitForSeconds(waitBetweenHitsTime);
    }

    public override void UseItem()
    {
        base.UseItem();

        if(Time.time - lastUseTime < useCooldown)
        {
            return; // still on cooldown
        }

        lastUseTime = Time.time;
        StartCoroutine(HandleHit());
    }

    private Collider[] colliders = new Collider[10]; // reusable array to avoid allocations
    private IEnumerator HandleHit()
    {

        var alreadyHit = new List<IDamageable>();
        
        animator.SetTrigger("DoAction");
        yield return waitForFirstHit;

        foreach(var hitPoint in hitPoints)
        {
            var hitPosition = transform.TransformPoint(hitPoint);
            var hits = Physics.OverlapSphereNonAlloc(hitPosition, hitRadius, colliders);

            for(int i = 0; i < hits; i++)
            {
                var hit = colliders[i];
                if(hit.TryGetComponent<IDamageable>(out var damageable) && !alreadyHit.Contains(damageable))
                {
                    damageable.TakeDamage(damage); // example damage value
                    alreadyHit.Add(damageable);
                }

                if(hit.TryGetComponent<MobHealth>(out var mobHealth))
                {
                    mobHealth.TakeDamage(damage, transform.position); // example damage value
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        foreach (var point in hitPoints)
        {
            Gizmos.DrawWireSphere(transform.TransformPoint(point), hitRadius);
        }
    }
}
