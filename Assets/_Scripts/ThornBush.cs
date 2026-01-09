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
   // 2. THE BOOM
    public void Explode()
    {
        if (hasExploded) return; 
        hasExploded = true;

        // A. Deal Damage & Trigger Chains
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                EnemyBase enemy = hit.GetComponent<EnemyBase>();
                if (enemy != null) enemy.TakeDamage(damage);
            }
            else if (hit.GetComponent<ThornBush>() != null)
            {
                if (hit.gameObject != gameObject) 
                {
                    hit.GetComponent<ThornBush>().Invoke("Explode", chainReactionDelay);
                }
            }
        }

        // B. Visuals & Sound
        if (explosionEffect) Instantiate(explosionEffect, transform.position, Quaternion.identity);

        // --- FIX IS HERE ---
        if (explosionSound) 
        {
            // Play at Camera position (Full Volume) instead of Bush position (Quiet/Silent)
            Vector3 soundPos = (Camera.main != null) ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(explosionSound, soundPos, 1.0f);
        }
        // -------------------

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