using UnityEngine;

public class ThornBush : MonoBehaviour
{
    [Header("Explosive Stats")]
    public float explosionRadius = 3.5f;
    public float damage = 50f; 
    public float chainReactionDelay = 0.2f; // Slight delay looks cooler

    [Header("Visuals")]
    public GameObject explosionEffect; // Drag a particle system or simple red circle sprite here
    public AudioClip explosionSound;

    private bool hasExploded = false;

    // 1. DETECT BULLET
    void OnTriggerEnter2D(Collider2D other)
    {
        // Make sure your Player's bullet prefab has the Tag "Bullet"
        if (other.CompareTag("Bullet")) 
        {
            Explode();
            Destroy(other.gameObject); // Destroy the bullet so it doesn't fly through
        }
    }

    // 2. THE BOOM
    public void Explode()
    {
        if (hasExploded) return; // Prevent infinite loops
        hasExploded = true;

        // A. Deal Damage & Trigger Chains
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var hit in hits)
        {
            // Damage Enemies
            if (hit.CompareTag("Enemy"))
            {
                EnemyBase enemy = hit.GetComponent<EnemyBase>();
                if (enemy != null) enemy.TakeDamage(damage);
            }
            // CHAIN REACTION: Trigger other bushes!
            else if (hit.GetComponent<ThornBush>() != null)
            {
                // Don't explode myself again
                if (hit.gameObject != gameObject) 
                {
                    // Delay the next explosion slightly for a cool "Wave" effect
                    hit.GetComponent<ThornBush>().Invoke("Explode", chainReactionDelay);
                }
            }
        }

        // B. Visuals & Sound
        if (explosionEffect) Instantiate(explosionEffect, transform.position, Quaternion.identity);
        if (explosionSound) AudioSource.PlayClipAtPoint(explosionSound, transform.position);

        // C. Remove the bush
        Destroy(gameObject);
    }

    // Editor Helper: Shows the red explosion circle so you can balance it
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}