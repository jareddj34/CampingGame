using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;


public class MobHealth : MonoBehaviour
{
    // Dropdown for mob types
    public enum MobType
    {
        Chicken,
        Deer,
    }
    public MobType mobType; // Set this in the Inspector for each mob prefab

    [SerializeField] private float m_MaxHealth = 4f; // chickens die in 4 hits like Minecraft

    public event Action<Vector3> OnDamaged;
    public event Action OnDied;

    public float Current { get; private set; }
    public bool IsDead => Current <= 0f;

    public AudioSource hitSound; // Sound to play when hit
    public AudioSource chickenHurtSound;
    public AudioClip[] chickenHurtClips;
    public AudioClip chickenDeathClip;

    private SkinnedMeshRenderer[] m_Renderers;
    private Color[] m_OriginalColors;
    private Coroutine m_FlashCoroutine;

    public GameObject chickenWingPrefab;
    public bool isBoss;

    private void Awake()
    {
        Current = m_MaxHealth;

        m_Renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        m_OriginalColors = new Color[m_Renderers.Length];
        for (int i = 0; i < m_Renderers.Length; i++)
        {
            m_OriginalColors[i] = m_Renderers[i].material.color;
        }

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
        if (chickenHurtSound != null && chickenHurtClips != null && chickenHurtClips.Length > 0)
        {
            chickenHurtSound.PlayOneShot(chickenHurtClips[UnityEngine.Random.Range(0, chickenHurtClips.Length)]);
        }
        PopOnHit(); // visual feedback for being hit
        FlashRed(); // flash red when hit

        if (IsDead)
        {

            if (m_FlashCoroutine != null)
                StopCoroutine(m_FlashCoroutine); // cancel the flash so it stays red
            SetMeshColor(Color.red);             // lock it red on death

            OnDied?.Invoke();

            // if(mobType == MobType.Chicken)
            // {
            //     KillCounter.Instance?.AddKill(); // Increment kill count when mob dies
            // }

            if (chickenHurtSound != null && chickenDeathClip != null)
            {
                chickenHurtSound.PlayOneShot(chickenDeathClip);
            }

            StartCoroutine(SpawnChickenWingsAfterDelay(0.8f)); // spawn chicken wings after a short delay

            // Simple death
            Destroy(gameObject, 1f);
        }
    }

    IEnumerator SpawnChickenWingsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if(isBoss)
        {
            for(int i = 0; i < 4; i++) // spawn 5 chicken wings on boss death
            {
                Instantiate(chickenWingPrefab, transform.position + Vector3.up * 0.8f, Quaternion.identity);
            }
        }
        else if(mobType == MobType.Chicken)
        {
            Instantiate(chickenWingPrefab, transform.position + Vector3.up * 0.8f, Quaternion.identity);
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

    public void FlashRed(float duration = 0.5f)
    {
        if (m_FlashCoroutine != null)
            StopCoroutine(m_FlashCoroutine);
        m_FlashCoroutine = StartCoroutine(FlashRedCoroutine(duration));
    }

    IEnumerator FlashRedCoroutine(float duration)
    {
        SetMeshColor(Color.red);
        yield return new WaitForSeconds(duration);
        if (!IsDead)
            ResetMeshColor();
    }

    private void SetMeshColor(Color color)
    {
        foreach (var r in m_Renderers)
            r.material.color = color;
    }

    private void ResetMeshColor()
    {
        for (int i = 0; i < m_Renderers.Length; i++)
            m_Renderers[i].material.color = m_OriginalColors[i];
    }
}
