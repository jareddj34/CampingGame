using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 10;
    public int currentHealth;
    public float regenDelay = 5f;
    public float regenRate = 5f;
    public float regenTickInterval = 0.1f;

    [Header("Death")]
    [SerializeField] private GameObject deathScreen;
    public bool isSurvival = false;
    public GameObject survivalDeathscreen;
    [SerializeField] private float deathAnimDuration = 1.2f;
    [SerializeField] private float deathDropAmount = 0.8f;
    [SerializeField] private float deathTiltAngle = 80f;

    [Header("Sounds")]
    public AudioSource audioSource;
    public AudioClip[] hurtSounds;
    public AudioClip deathSound;

    private Coroutine regenCoroutine;
    private bool isDead = false;

    public Image healthBar;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        healthBar.fillAmount = (float)currentHealth / maxHealth;

        if (regenCoroutine != null)
            StopCoroutine(regenCoroutine);
        regenCoroutine = StartCoroutine(RegenAfterDelay());

        if (currentHealth <= 0)
            Die();
        
        if (audioSource != null && hurtSounds != null && hurtSounds.Length > 0)
        {
            audioSource.PlayOneShot(hurtSounds[UnityEngine.Random.Range(0, hurtSounds.Length)]);
        }
    }

    private IEnumerator RegenAfterDelay()
    {
        yield return new WaitForSeconds(regenDelay);

        float regenAccumulator = 0f;

        while (currentHealth < maxHealth)
        {
            regenAccumulator += regenRate * regenTickInterval;

            int healthToAdd = Mathf.FloorToInt(regenAccumulator);
            if (healthToAdd > 0)
            {
                regenAccumulator -= healthToAdd;
                currentHealth = Mathf.Min(currentHealth + healthToAdd, maxHealth);
                healthBar.fillAmount = (float)currentHealth / maxHealth;
            }

            yield return new WaitForSeconds(regenTickInterval);
        }

        regenCoroutine = null;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        audioSource.PlayOneShot(deathSound);

        // Stop regen
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
            regenCoroutine = null;
        }

        // Disabling PlayerController also unlocks the cursor via its OnDisable
        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null) pc.enabled = false;

        // Disable CharacterController so it doesn't fight the death animation
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        InputHandler ih = GetComponent<InputHandler>();
        if(ih != null) ih.enabled = false;

        InteractionManager im = GetComponent<InteractionManager>();
        if(im != null) im.enabled = false;

        BoomerangLauncher bl = GetComponent<BoomerangLauncher>();
        if(bl != null) bl.enabled = false;

        UnityEngine.InputSystem.PlayerInput playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null) playerInput.enabled = false;

        StartCoroutine(DeathAnimation());
    }

    private IEnumerator DeathAnimation()
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 endPos = startPos + Vector3.down * deathDropAmount;
        // Tip forward and slightly sideways, like a person collapsing
        Quaternion endRot = startRot * Quaternion.Euler(0f, 0f, deathTiltAngle);

        float elapsed = 0f;
        while (elapsed < deathAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / deathAnimDuration;
            // Smoothstep so the fall eases in and out
            float smoothT = t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(startPos, endPos, smoothT);
            transform.rotation = Quaternion.Lerp(startRot, endRot, smoothT);

            yield return null;
        }

        transform.position = endPos;
        transform.rotation = endRot;

        if (deathScreen != null && !isSurvival) {
            deathScreen.SetActive(true);
        }
        else if(survivalDeathscreen != null) {
            survivalDeathscreen.SetActive(true);
        }
    }
}