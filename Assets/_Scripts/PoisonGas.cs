using UnityEngine;

public class PoisonGas : MonoBehaviour
{
    public float damagePerSecond = 10.0f; // High damage
    public float moveSpeed = 4.0f; 
    public float lifetime = 2.0f; 

    void Start()
    {
        // Destroy gas after a few seconds
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Float forward (Spray effect)
        transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                // Simple Damage - No Buffs
                player.TakeDamage(damagePerSecond * Time.deltaTime);
                
                // Optional: Flash player red?
                // player.GetComponent<SpriteRenderer>().color = Color.red;
            }
        }
    }
}