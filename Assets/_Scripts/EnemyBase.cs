using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    public float speed = 2f;
    public float health = 30f;
    protected Transform target; // The Player

    [Header("Audio")]
    public AudioClip deathSplatterSound;

    [Header("Volatile Garden")]
    public GameObject explosiveBushPrefab; // DRAG YOUR BUSH PREFAB HERE
    public float dropChance = 20f; // 20% Chance to drop a mine

    protected virtual void Start()
    {
        // Find player once at start to save performance
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) target = p.transform;
    }

    protected virtual void Update()
    {
        // Default behavior: Walk to player
        if(target != null)
        {
            transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
        }
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        if(health <= 0) Die();
    }

    public void Die()
    {
        // --- 1. VOLATILE GARDEN LOGIC ---
        // Roll the dice (0 to 100). If less than 20, drop a bush.
        if (Random.Range(0f, 100f) < dropChance)
        {
            if (explosiveBushPrefab != null)
            {
                Instantiate(explosiveBushPrefab, transform.position, Quaternion.identity);
            }
        }

        // --- 2. BIOMASS ABSORPTION LOGIC ---
        if (target != null)
        {
            PlayerController pScript = target.GetComponent<PlayerController>();
            
            // Check if we are close enough to be "eaten"
            if (pScript != null)
            {
                float distance = Vector2.Distance(transform.position, target.position);
                if (distance <= pScript.absorptionRadius)
                {
                    pScript.AddBiomass(10f);
                }
            }
        }

        // --- 3. AUDIO LOGIC ---
        if(deathSplatterSound)
        {
            AudioSource.PlayClipAtPoint(deathSplatterSound, transform.position);
        }

        // --- 4. CLEANUP ---
        Destroy(gameObject);
    }
}