using UnityEngine;

public class GuardEnemy : EnemyBase
{
    [Header("Guard Combat")]
    public float attackRange = 1.5f; // How close to stand before hitting
    public float damage = 10f;
    public float attackRate = 1.0f; // Time between hits
    private float nextAttackTime = 0f;

    protected override void Update()
    {
        // 1. Run normal movement (Move towards Player)
        // We override Update, but we still want the movement logic from Base
        // However, we want to STOP moving if we are in attack range.
        
        if (target != null)
        {
            float distance = Vector2.Distance(transform.position, target.position);

            if (distance > attackRange)
            {
                // Too far? Keep walking (Use base movement logic)
                transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
            }
            else
            {
                // Close enough? Attack!
                if (Time.time >= nextAttackTime)
                {
                    AttackPlayer();
                }
            }
        }
    }

   void AttackPlayer()
    {
        nextAttackTime = Time.time + attackRate;
        
        // FIXED: Using PlayerController instead of PlayerPlant
        PlayerController player = target.GetComponent<PlayerController>();
        if (player != null)
        {
            player.TakeDamage(damage);
        }
    }
}