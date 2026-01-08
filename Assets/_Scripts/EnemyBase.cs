using UnityEngine;
using System.Collections;

public class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    public float speed = 2f;
    public float health = 30f;
    public float touchDamageToPlayer = 10f; 
    
    protected Transform target; 
    [Header("Blood VFX & SFX")]
    public GameObject bloodEffectPrefab; // Drag your Blood Particle Prefab here
    public AudioClip hitSound;           // Drag a "Squish" or "Hit" sound here

    // --- FIXED: ADDED MISSING AUDIO CLIP VARIABLE ---
    [Header("Audio")]
    public AudioClip deathSplatterSound; 
    // ------------------------------------------------

    [Header("Volatile Garden")]
    public GameObject explosiveBushPrefab; 
    public float dropChance = 20f; 

    [Header("Fungal Disease")]
    public bool isInfected = false;
    public Color infectedColor = Color.green;
    public float timeUntilDeath = 3.0f; // Dies in a few seconds

    protected virtual void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) target = p.transform;
    }

    protected virtual void Update()
    {
        // 1. IF INFECTED: DO NOTHING (Stand still and wait to die)
        if (isInfected)
        {
            return; 
        }

        // 2. IF NORMAL: CHASE PLAYER
        if(target != null)
        {
            transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
        }
    }

    // --- INFECTION LOGIC (Limited to 4 jumps) ---
    public void Infect(int spreadLimit)
    {
        if (isInfected) return; // Already sick
        
        isInfected = true;
        
        // 1. Change Color
        GetComponent<SpriteRenderer>().color = infectedColor;

        // 2. Trigger Scientist Hook (Stop working)
        OnInfected();

        // 3. Start Spreading (Only if we have spreads left)
        if (spreadLimit > 0)
        {
            StartCoroutine(SpreadRoutine(spreadLimit));
        }

        // 4. Die after time
        StartCoroutine(DeathTimer());
    }

    IEnumerator SpreadRoutine(int remainingSpreads)
    {
        // Try to spread every 0.5 seconds until death
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            
            // Look for neighbors to infect
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 3.0f);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Enemy"))
                {
                    EnemyBase neighbor = hit.GetComponent<EnemyBase>();
                    
                    // Infect neighbor with ONE LESS spread count
                    if (neighbor != null && !neighbor.isInfected)
                    {
                        neighbor.Infect(remainingSpreads - 1);
                    }
                }
            }
        }
    }

    public void TakeDamage(float amount)
    {
        health -= amount;

        // --- 1. BLOOD VISUALS ---
        if (bloodEffectPrefab != null)
        {
            // Instantiate blood at the enemy's position
            Instantiate(bloodEffectPrefab, transform.position, Quaternion.identity);
        }

        // --- 2. HIT AUDIO ---
        if (hitSound != null)
        {
            // PlayClipAtPoint creates a temporary audio source that dies after playing
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }
        
        // --- 3. INFECTION LOGIC (Keep existing) ---
        if (!isInfected && target != null)
        {
            PlayerController pc = target.GetComponent<PlayerController>();
            if (pc != null && pc.isFungal)
            {
                if (Random.value <= 0.10f) 
                {
                    // Spread to 3 neighbors
                    Infect(3); 
                }
            }
        }

        if(health <= 0) Die();
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        // INFECTED ENEMIES ARE HARMLESS
        if (isInfected) return;

        // Normal enemies hurt the player
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null) player.TakeDamage(touchDamageToPlayer);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Same logic for Triggers
        if (isInfected) return;

        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null) player.TakeDamage(touchDamageToPlayer);
        }
    }

    protected virtual void OnInfected() { }

    IEnumerator DeathTimer()
    {
        yield return new WaitForSeconds(timeUntilDeath);
        Die();
    }

    public void Die()
    {
        if (Random.Range(0f, 100f) < dropChance)
        {
            if (explosiveBushPrefab != null) Instantiate(explosiveBushPrefab, transform.position, Quaternion.identity);
        }

        if (target != null)
        {
            PlayerController pScript = target.GetComponent<PlayerController>();
            if (pScript != null)
            {
                float d = Vector2.Distance(transform.position, target.position);
                if (d <= pScript.absorptionRadius) pScript.AddBiomass(10f);
            }
        }
        
        // Play Sound
        if(deathSplatterSound) AudioSource.PlayClipAtPoint(deathSplatterSound, transform.position);
        
        Destroy(gameObject);
    }
}