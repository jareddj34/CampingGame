using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;


public class MobHealth : MonoBehaviour
{
    [SerializeField] private float m_MaxHealth = 4f; // chickens die in 4 hits like Minecraft

    public event Action<Vector3> OnDamaged;

    public float Current { get; private set; }
    public bool IsDead => Current <= 0f;

    public AudioSource hitSound; // Sound to play when hit

    private void Awake()
    {
        Current = m_MaxHealth;
    }

    public void TakeDamage(float amount, Vector3 attackerPosition)
    {
        if (IsDead) return;

        Current = Mathf.Max(Current - amount, 0f);
        OnDamaged?.Invoke(attackerPosition);

        if (hitSound != null)
        {
            hitSound.Play();
        }
        PopOnHit(); // visual feedback for being hit

        if (IsDead)
        {

            KillCounter.Instance?.AddKill(); // Increment kill count when mob dies

            // Simple death
            Destroy(gameObject);
        }
    }

    // Function to make the mob pop up when hit
    public void PopOnHit(float duration = 0.2f, float intensity = 0.3f)
    {
        StartCoroutine(PopCoroutine(duration, intensity));
    }

    IEnumerator PopCoroutine(float duration, float intensity)
    {
        Vector3 originalScale = transform.localScale;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float scaleMultiplier = 1 + Mathf.Sin((timer / duration) * Mathf.PI) * intensity;
            transform.localScale = originalScale * scaleMultiplier;
            yield return null;
        }

        transform.localScale = originalScale;
    }
}
