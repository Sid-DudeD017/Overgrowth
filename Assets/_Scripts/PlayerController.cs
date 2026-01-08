using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq; 

public class PlayerController : MonoBehaviour
{
    public enum UpgradeType { Hydra, Vampiric, Titan, Domain }

    [System.Serializable]
    public struct UpgradeOption
    {
        public string title;
        public string description;
        public UpgradeType type;
    }

    [Header("Stats")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float healthCostPerShot = 2.0f; // Biomass Ammo Cost
    
    [Header("Root Regeneration")]
    public float regenRate = 10f;       // HP healed per second
    public float regenDelay = 1.0f;     // Wait time before healing
    private float lastMoveTime;         // Tracks movement

    [Header("RPG Stats")]
    public int growthLevel = 1;
    public int maxGrowthLevel = 10; 
    public float absorptionRadius = 4.0f; 
    public float bulletDamage = 10f;
    
    [Header("Shooting")]
    public float fireRate = 0.1f; // Semi-auto click speed
    private float nextFireTime;
    public Transform shootPoint;
    public GameObject projectilePrefab;
    public float bulletHoming = 0f;

    [Header("Whip Stats")]
    public float whipRange = 3.5f;
    public float whipDamage = 50f;
    public float whipCooldown = 2.0f;
    private float nextWhipTime = 0f;

    [Header("Mutation: Hydra (Chomper)")]
    public GameObject hydraHeadPrefab; 
    public float chompRange = 2.0f;       // Distance to eat enemy
    public float digestionTime = 3.0f;    // Time to process food
    public float healFromDigestion = 20f; // Reward for eating
    private List<HydraTurret> activeHydraHeads = new List<HydraTurret>();

    [Header("Mutation: Vampiric")]
    public bool isVampiric = false;
    public float healOnKillAmount = 5f;

    [Header("XP System")]
    public float currentXP = 0f;
    public float xpToNextLevel = 30f; 
    
    [Header("Steroid Gas")]
    public bool isInSteroidGas = false;
    public float steroidChance = 40f; 
    private float steroidTimer = 0f; // Tracks duration

    [Header("Upgrade Counts (Limits)")]
    public int hydraCount = 0;
    public int maxHydraCount = 4; 
    public int domainCount = 0;
    public int maxDomainCount = 3;

    [Header("References")]
    public GameUIManager uiManager; 
    public UnityEvent OnDeath;

    [Header("Audio")]
    public AudioSource playerAudio;
    public AudioClip shootSound;
    public AudioClip whipSound;
    public AudioClip growthSound; 

    private Camera mainCam;
    private List<UpgradeOption> currentRoundOptions = new List<UpgradeOption>();

    // MASTER LIST
    private List<UpgradeOption> allUpgrades = new List<UpgradeOption>()
    {
        new UpgradeOption { title = "CHOMPER", description = "+1 Chomping Head", type = UpgradeType.Hydra },
        new UpgradeOption { title = "VAMPIRE", description = "Heal +5HP on Kill", type = UpgradeType.Vampiric },
        new UpgradeOption { title = "TITAN", description = "Size +50%\nMax HP x2\n+2 Max Hydras", type = UpgradeType.Titan },
        new UpgradeOption { title = "DOMAIN", description = "Whip Rng +50%\nAbsorb +50%", type = UpgradeType.Domain }
    };

    void Start()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        if (currentHealth <= 0) return;

        // 1. STEROID GAS TIMER
        if (steroidTimer > 0)
        {
            steroidTimer -= Time.deltaTime;
            isInSteroidGas = true;
        }
        else
        {
            isInSteroidGas = false;
        }

        // 2. REGENERATION LOGIC
        float moveInput = Input.GetAxisRaw("Horizontal") + Input.GetAxisRaw("Vertical");

        if (moveInput != 0) // Moving
        {
            lastMoveTime = Time.time; 
        }
        else // Standing Still
        {
            if (Time.time >= lastMoveTime + regenDelay)
            {
                if (currentHealth < maxHealth)
                {
                    currentHealth += regenRate * Time.deltaTime;
                    if (currentHealth > maxHealth) currentHealth = maxHealth;
                    if(uiManager && uiManager.healthSlider) uiManager.healthSlider.value = currentHealth / maxHealth;
                }
            }
        }

        // 3. AIMING
        RotateTowardsMouse();

        // 4. SHOOTING (Left Click)
        if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime)
        {
            Shoot(shootPoint, bulletDamage, bulletHoming);
        }

        // 5. WHIP (Space)
        if (Input.GetKeyDown(KeyCode.Space) && Time.time >= nextWhipTime)
        {
            VineWhip();
        }

        // 6. HYDRA LOGIC (Tick updates)
        // Convert list to array safely to avoid modification errors during loop
        for(int i = 0; i < activeHydraHeads.Count; i++)
        {
            activeHydraHeads[i].Tick(Time.time);
        }
    }

    // --- HELPER FUNCTIONS ---

    public void RefreshSteroidDuration(float duration)
    {
        steroidTimer = duration;
        isInSteroidGas = true;
    }

    void RotateTowardsMouse()
    {
        Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = mousePos - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90); 
    }

    public void Shoot(Transform origin, float dmg, float homing)
    {
        // 1. BIOMASS COST
        if(origin == shootPoint)
        {
            if (currentHealth <= healthCostPerShot + 1) return; 
            TakeDamage(healthCostPerShot);
            nextFireTime = Time.time + fireRate;
        }

        if (projectilePrefab == null || origin == null) return;

        // 2. STEROID LOGIC
        // Using "isInSteroidGas" directly for testing (Always true if in gas)
        bool triggerSteroid = isInSteroidGas; 

        if (triggerSteroid)
        {
            float boostDamage = dmg * 1.5f; 
            float spreadAngle = 25f; // Increased angle for visibility
            float sideOffset = 0.5f; // Distance between bullets
            
            // A. Center Bullet (Normal)
            CreateBullet(origin.position, origin.rotation, boostDamage, homing);
            
            // B. Right Bullet (Shifted Right + Rotated)
            // "origin.right" is the direction to the side of the player
            Vector3 rightPos = origin.position + (origin.right * sideOffset);
            Quaternion rightRot = origin.rotation * Quaternion.Euler(0, 0, -spreadAngle);
            CreateBullet(rightPos, rightRot, boostDamage, homing);

            // C. Left Bullet (Shifted Left + Rotated)
            Vector3 leftPos = origin.position - (origin.right * sideOffset);
            Quaternion leftRot = origin.rotation * Quaternion.Euler(0, 0, spreadAngle);
            CreateBullet(leftPos, leftRot, boostDamage, homing);

            Debug.Log("TRIPLE SHOT FIRED!");
        }
        else
        {
            // NORMAL SHOT
            CreateBullet(origin.position, origin.rotation, dmg, homing);
        }

        // Sound
        if(playerAudio && shootSound && origin == shootPoint) 
        {
            playerAudio.pitch = Random.Range(0.9f, 1.1f);
            playerAudio.PlayOneShot(shootSound);
        }
    }
    void CreateBullet(Vector3 pos, Quaternion rot, float damage, float homing)
    {
        GameObject bullet = Instantiate(projectilePrefab, pos, rot);
        PlantProjectile pp = bullet.GetComponent<PlantProjectile>();
        if(pp != null)
        {
            pp.damage = damage;
            pp.homingStrength = homing; 
        }
    }

    void VineWhip()
    {
        nextWhipTime = Time.time + whipCooldown;
        if(playerAudio && whipSound) playerAudio.PlayOneShot(whipSound);
        if(uiManager) uiManager.UpdateXPUI(currentXP, xpToNextLevel); 

        Collider2D[] enemiesHit = Physics2D.OverlapCircleAll(transform.position, whipRange);
        foreach (Collider2D col in enemiesHit)
        {
            if (col.CompareTag("Enemy"))
            {
                EnemyBase enemy = col.GetComponent<EnemyBase>();
                if (enemy != null) enemy.TakeDamage(whipDamage);
            }
        }
    }

    public void AddBiomass(float amount)
    {
        if (isVampiric)
        {
            currentHealth += healOnKillAmount;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
            if (uiManager && uiManager.healthSlider) uiManager.healthSlider.value = currentHealth / maxHealth;
        }

        currentXP += amount;
        if(uiManager) uiManager.UpdateXPUI(currentXP, xpToNextLevel);

        if (currentXP >= xpToNextLevel)
        {
            currentXP = 0;
            xpToNextLevel *= 1.4f; 
            PrepareLevelUp();
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if(uiManager && uiManager.healthSlider) uiManager.healthSlider.value = currentHealth / maxHealth;
        if(currentHealth <= 0 && OnDeath != null) OnDeath.Invoke(); 
    }

    public void HealFromDigestion()
    {
        currentHealth += healFromDigestion;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        
        if(uiManager && uiManager.healthSlider) 
            uiManager.healthSlider.value = currentHealth / maxHealth;

        if(playerAudio && growthSound) playerAudio.PlayOneShot(growthSound);
        Debug.Log("DIGESTION COMPLETE: HEALED!");
    }

    // --- LEVEL UP SYSTEM ---
    void PrepareLevelUp()
    {
        if (growthLevel >= maxGrowthLevel)
        {
            TriggerWin();
            return;
        }

        List<UpgradeOption> validUpgrades = new List<UpgradeOption>();
        foreach(var upgrade in allUpgrades)
        {
            if (upgrade.type == UpgradeType.Hydra && hydraCount >= maxHydraCount) continue;
            if (upgrade.type == UpgradeType.Domain && domainCount >= maxDomainCount) continue;
            validUpgrades.Add(upgrade);
        }

        List<UpgradeOption> shuffled = validUpgrades.OrderBy(x => System.Guid.NewGuid()).ToList();
        int optionsToTake = Mathf.Min(3, shuffled.Count);
        currentRoundOptions = shuffled.Take(optionsToTake).ToList();

        List<string> uiTexts = new List<string>();
        foreach(var opt in currentRoundOptions) uiTexts.Add($"{opt.title}\n{opt.description}");

        if(uiManager) uiManager.ShowLevelUpOptions(uiTexts, OnUpgradeSelected);
    }

    void OnUpgradeSelected(int choiceIndex)
    {
        if(choiceIndex >= currentRoundOptions.Count) return;
        UpgradeOption selected = currentRoundOptions[choiceIndex];
        ApplyUpgrade(selected.type);
    }

    void ApplyUpgrade(UpgradeType type)
    {
        growthLevel++;
        currentHealth = maxHealth; 
        AudioManager.instance.PlayGrowth();

        switch (type)
        {
            case UpgradeType.Hydra:
                hydraCount++;
                SpawnHydraHead(); 
                Debug.Log($"Hydra Added! ({hydraCount}/{maxHydraCount})");
                break;

            case UpgradeType.Vampiric:
                isVampiric = true;
                healOnKillAmount += 5f; 
                absorptionRadius *= 1.2f; 
                break;

            case UpgradeType.Titan:
                transform.localScale *= 1.5f; 
                maxHealth *= 2.0f; 
                currentHealth = maxHealth;
                maxHydraCount += 2;
                ResizeAndRepositionHydras();
                break;
                
            case UpgradeType.Domain:
                domainCount++;
                absorptionRadius *= 1.5f; 
                whipRange *= 1.5f; 
                whipDamage *= 1.5f;
                break;
        }
    }

    void SpawnHydraHead()
    {
        if(hydraHeadPrefab == null) return;

        GameObject newHead = Instantiate(hydraHeadPrefab, transform.position, Quaternion.identity);
        newHead.transform.SetParent(transform);

        HydraTurret turret = new HydraTurret();
        turret.transform = newHead.transform;
        turret.owner = this;
        activeHydraHeads.Add(turret);

        ResizeAndRepositionHydras();
    }

    void ResizeAndRepositionHydras()
    {
        if (activeHydraHeads.Count == 0) return;

        float distance = 1.2f; 
        float angleStep = 360f / activeHydraHeads.Count; 

        for (int i = 0; i < activeHydraHeads.Count; i++)
        {
            Transform headT = activeHydraHeads[i].transform;
            float angle = i * angleStep;
            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * distance;
            float y = Mathf.Sin(angle * Mathf.Deg2Rad) * distance;
            
            headT.localPosition = new Vector3(x, y, 0);

            float inverseScale = 1f / transform.localScale.x; 
            headT.localScale = new Vector3(inverseScale, inverseScale, 1f);
        }
    }

    void TriggerWin()
    {
        transform.localScale *= 5.0f; 
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach(var e in enemies) Destroy(e);
        if(uiManager) uiManager.ShowWinScreen();
        AudioManager.instance.PlayWin();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, whipRange);
    }
}

// --- HYDRA TURRET CLASS (Updated for Chomper Logic) ---
public class HydraTurret
{
    public Transform transform;
    public PlayerController owner;
    
    private float digestionTimer = 0f;
    private bool isDigesting = false;
    private SpriteRenderer sr; 

    public void Tick(float currentTime)
    {
        if(transform == null) return;
        if(sr == null) sr = transform.GetComponent<SpriteRenderer>();

        // STATE 1: DIGESTING
        if (isDigesting)
        {
            digestionTimer -= Time.deltaTime; 
            if(sr) sr.color = new Color(0.6f, 0.4f, 0.2f); // Brown

            if (digestionTimer <= 0)
            {
                isDigesting = false;
                owner.HealFromDigestion(); 
                if(sr) sr.color = Color.white; 
            }
            return; 
        }

        // STATE 2: HUNGRY (Chomp Check)
        GameObject closestEnemy = null;
        float closestDist = owner.chompRange; 
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, owner.chompRange);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                float d = Vector2.Distance(transform.position, hit.transform.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    closestEnemy = hit.gameObject;
                }
            }
        }

        if (closestEnemy != null)
        {
            EnemyBase enemyScript = closestEnemy.GetComponent<EnemyBase>();
            if(enemyScript != null) enemyScript.Die(); 
            else GameObject.Destroy(closestEnemy);

            isDigesting = true;
            digestionTimer = owner.digestionTime;
        }
    }
}