using UnityEngine;

public class PlantProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 10f;
    public float damage = 10f;
    public float lifeTime = 3f;
    
    // NEW: Homing Stat (Set by Player when spawning)
    public float homingStrength = 0f; 

    private Transform target;

    void Start()
    {
        Destroy(gameObject, lifeTime);
        FindTarget();
    }

    void Update()
    {
        if (homingStrength > 0 && target != null)
        {
            // Homing Logic: Rotate smoothly towards target
            Vector2 direction = (Vector2)target.position - (Vector2)transform.position;
            direction.Normalize();
            
            float rotateAmount = Vector3.Cross(direction, transform.up).z;
            
            // "slight" homing means we subtract rotation slowly
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90;
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
            
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, homingStrength * 100 * Time.deltaTime);
        }

        // Always move "Up" relative to self
        transform.Translate(Vector2.up * speed * Time.deltaTime);
    }

    void FindTarget()
    {
        // Simple optimization: Find closest enemy
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float closestDist = Mathf.Infinity;
        
        foreach (GameObject enemy in enemies)
        {
            float d = Vector2.Distance(transform.position, enemy.transform.position);
            if (d < closestDist)
            {
                closestDist = d;
                target = enemy.transform;
            }
        }
    }
    
    // ... Keep your OnTriggerEnter2D logic same as before ...
    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        if (hitInfo.CompareTag("Enemy"))
        {
             EnemyBase enemy = hitInfo.GetComponent<EnemyBase>();
             if(enemy != null) enemy.TakeDamage(damage);
             Destroy(gameObject);
        }
    }
}