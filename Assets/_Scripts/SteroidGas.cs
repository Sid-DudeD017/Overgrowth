using UnityEngine;

public class SteroidGas : MonoBehaviour
{
    public float damagePerSecond = 5.0f; 
    public float moveSpeed = 4.0f; 
    public float lifetime = 2.0f; // Disappears automatically after this time

    void Start()
    {
        // Destroy gas after 2 seconds (so it disappears "just ahead" or after missing)
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Float forward
        transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
    }

   void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damagePerSecond * Time.deltaTime);
                
                // THIS LINE IS CRITICAL:
                player.RefreshSteroidDuration(0.2f); 
            }
        }
    }
}