using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI; // <--- ADD THIS! REQUIRED FOR UI IMAGES
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public enum UpgradeType { Hydra, Vampiric, Titan, Domain, Fungal, MultiShot }

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
    public float healthCostPerShot = 2.0f; 
    
    [Header("Nerfed: Root Regeneration")]
    public float regenRate = 3f;        
    public float regenDelay = 2.0f;     
    private float lastMoveTime;         

    [Header("Nerfed: Whip System (Stamina)")]
    public int maxWhipCharges = 2;
    public int currentWhipCharges = 2;
    public float whipRechargeTime = 5.0f; 
    private float nextWhipChargeTimer = 0f;
    private float nextWhipTime = 0f; 
    [Header("Whip UI")]
    public Image[] whipIcons; // Drag your 2 Image objects here in Inspector
    public Color iconActiveColor = Color.green;
    public Color iconEmptyColor = new Color(0.5f, 0.5f, 0.5f, 0.3f); // Faded Grey
    // ------------------------

    [Header("RPG Stats")]
    public int growthLevel = 1;
    public int maxGrowthLevel = 10; 
    public float absorptionRadius = 4.0f; 
    public float bulletDamage = 10f;
    
    [Header("Shooting")]
    public float fireRate = 0.1f; 
    private float nextFireTime;
    public Transform shootPoint;      
    public Transform shootPointLeft;  
    public Transform shootPointRight; 
    public GameObject projectilePrefab;
    public float bulletHoming = 0f;

    [Header("Mutation: MultiShot")]
    public bool isMultiShot = false; 

    [Header("Mutation: Fungal")]
    public bool isFungal = false; 

    [Header("Mutation: Hydra")]
    public GameObject hydraHeadPrefab; 
    public float chompRange = 2.0f;       
    public float digestionTime = 3.0f;    
    public float healFromDigestion = 15f; 
    // --- ADD THESE TO CONTROL THE GAP AND SIZE ---
    public float hydraBaseDistance = 0.5f; // Smaller number = Closer to body
    public float hydraHeadScale = 1.0f;    // Bigger number = Bigger heads
    public float hydraRotationOffset = -90f;
    // ---------------------------------------------
    private List<HydraTurret> activeHydraHeads = new List<HydraTurret>();

    [Header("Mutation: Vampiric")]
    public bool isVampiric = false;
    // CHANGE: Start at 0. (Upgrade 1 adds 2 = 2 Total). (Upgrade 2 adds 2 = 4 Total).
    public float healOnKillAmount = 0f; 
    private float nextVampireHealTime = 0f;
    [Header("XP System")]
    public float currentXP = 0f;
    public float xpToNextLevel = 30f; 
    // --- FIXED: ADDED MISSING WHIP VARIABLES HERE ---
    [Header("Whip Stats")]
    public float whipRange = 3.5f;
    public float whipDamage = 50f;

    [Header("Defense")]
    public float invincibilityDuration = 0.5f; 
    private float nextDamageTime = 0f;         

    [Header("Upgrade Counts (Strict Limits)")]
    public int hydraCount = 0;
    public int maxHydraCount = 4; // Max 4 Chompers
    
    public int domainCount = 0;
    public int maxDomainCount = 2; // Max 2 Domain Expansions

    // --- NEW: TITAN LIMIT ---
    public int titanCount = 0;
    public int maxTitanCount = 2; // Max 2 Titans
    // ------------------------

    [Header("References")]
    public GameUIManager uiManager; 
    public UnityEvent OnDeath;

    [Header("Audio")]
    public AudioSource playerAudio;
    public AudioClip shootSound;
    public AudioClip whipSound;
    public AudioClip growthSound; 
    public Animator animator; 
    public GameObject whipVFXPrefab; 

    private Camera mainCam;
    private List<UpgradeOption> currentRoundOptions = new List<UpgradeOption>();
    private List<UpgradeOption> allUpgrades = new List<UpgradeOption>()
    {
        new UpgradeOption { title = "CHOMPER", description = "+1 Chomping Head", type = UpgradeType.Hydra },
        new UpgradeOption { title = "VAMPIRE", description = "Heal +2HP on Kill", type = UpgradeType.Vampiric },
        new UpgradeOption { title = "TITAN", description = "Size +50%\nMax HP x2\n+2 Max Hydras", type = UpgradeType.Titan },
        new UpgradeOption { title = "DOMAIN", description = "Whip Rng +50%\nAbsorb +50%", type = UpgradeType.Domain },
        new UpgradeOption { title = "FUNGAL", description = "Infect enemies.\nThey spread virus & die.", type = UpgradeType.Fungal },
        new UpgradeOption { title = "TRIPLE SHOT", description = "Fire 3 bullets parallel.\nHigh accuracy.", type = UpgradeType.MultiShot }
    };

    void Start()
    {
        mainCam = Camera.main;
        currentWhipCharges = maxWhipCharges;
        UpdateWhipUI(); // <--- ADD THIS
    }

    void Update()
    {
        if (currentHealth <= 0) return;

        // 1. WHIP RECHARGE
       if (currentWhipCharges < maxWhipCharges)
        {
            nextWhipChargeTimer += Time.deltaTime;
            if (nextWhipChargeTimer >= whipRechargeTime)
            {
                currentWhipCharges++;
                nextWhipChargeTimer = 0f;
                UpdateWhipUI(); // <--- UPDATE UI ON RECHARGE
                Debug.Log($"Whip Recharged! ({currentWhipCharges}/{maxWhipCharges})");
            }
        }
        // 2. REGENERATION
        float moveInput = Input.GetAxisRaw("Horizontal") + Input.GetAxisRaw("Vertical");
        if (moveInput != 0) lastMoveTime = Time.time; 
        else if (Time.time >= lastMoveTime + regenDelay && currentHealth < maxHealth)
        {
            currentHealth += regenRate * Time.deltaTime;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
            if(uiManager && uiManager.healthSlider) uiManager.healthSlider.value = currentHealth / maxHealth;
        }

        RotateTowardsMouse();

        // 3. SHOOTING
        if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime)
        {
            Shoot(shootPoint, bulletDamage, bulletHoming);
        }

        // 4. WHIP
       if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Time.time >= nextWhipTime)
            {
                if (currentWhipCharges > 0)
                {
                    VineWhip();
                    currentWhipCharges--; 
                    UpdateWhipUI(); // <--- UPDATE UI ON USE
                    nextWhipTime = Time.time + 0.5f; 
                }
                
            }
        }
        // 5. HYDRA LOGIC
        for(int i = 0; i < activeHydraHeads.Count; i++) activeHydraHeads[i].Tick(Time.time);
    }

    public void Shoot(Transform origin, float dmg, float homing)
    {
        if (currentHealth <= healthCostPerShot + 1) return; 

        if(animator != null) animator.SetTrigger("Shoot");

        TakeDamage(healthCostPerShot); 
        nextFireTime = Time.time + fireRate;

        if (isMultiShot) 
        {
            // FIX: Using shootPoint.rotation allows you to rotate the point in the Editor
            CreateBullet(shootPoint.position, shootPoint.rotation, dmg, homing);
            
            if (shootPointLeft) 
                CreateBullet(shootPointLeft.position, shootPointLeft.rotation, dmg, homing);
            
            if (shootPointRight) 
                CreateBullet(shootPointRight.position, shootPointRight.rotation, dmg, homing);
        }
        else
        {
            // FIX: Using shootPoint.rotation
            CreateBullet(shootPoint.position, shootPoint.rotation, dmg, homing);
        }

        if(playerAudio && shootSound) 
        {
            playerAudio.pitch = Random.Range(0.9f, 1.1f);
            playerAudio.PlayOneShot(shootSound);
        }
    }

    void VineWhip()
    {
        if(playerAudio && whipSound) playerAudio.PlayOneShot(whipSound);


        // Spawn the green vines visual at player's position
       if (whipVFXPrefab != null)
        {
            // 1. Create the object
            GameObject vfx = Instantiate(whipVFXPrefab, transform.position, Quaternion.identity);
            
            // 2. Scale it up based on Domain Expansion Level
            // If domainCount is 0, scale is 1.0 (Normal)
            // If domainCount is 1, scale is 1.5 (Big)
            // If domainCount is 2, scale is 2.25 (Huge)
            float scaleMultiplier = Mathf.Pow(1.5f, domainCount); 
            vfx.transform.localScale = new Vector3(scaleMultiplier, scaleMultiplier, 1f);
        }
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

    void CreateBullet(Vector3 pos, Quaternion rot, float damage, float homing)
    {
        if(projectilePrefab == null) return;
        GameObject bullet = Instantiate(projectilePrefab, pos, rot);
        PlantProjectile pp = bullet.GetComponent<PlantProjectile>();
        if(pp != null) { pp.damage = damage; pp.homingStrength = homing; }
    }

    void RotateTowardsMouse()
    {
        if (mainCam == null) mainCam = Camera.main;
        Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = mousePos - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // --- FIX IS HERE ---
        // Changed from "- 90" to "+ 90" to flip it 180 degrees.
        transform.rotation = Quaternion.Euler(0, 0, angle + 90); 
    }

    public void HealFromDigestion()
    {
        currentHealth += healFromDigestion;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        if(uiManager && uiManager.healthSlider) uiManager.healthSlider.value = currentHealth / maxHealth;
        if(playerAudio && growthSound) playerAudio.PlayOneShot(growthSound);
    }

    public void AddBiomass(float amount)
    {
        if (isVampiric && Time.time >= nextVampireHealTime)
        {
            currentHealth += healOnKillAmount; 
            if (currentHealth > maxHealth) currentHealth = maxHealth;
            if (uiManager && uiManager.healthSlider) uiManager.healthSlider.value = currentHealth / maxHealth;
            nextVampireHealTime = Time.time + 0.1f; 
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
        if (Time.time < nextDamageTime) return; // Invincibility Check

        currentHealth -= amount;
        nextDamageTime = Time.time + invincibilityDuration;
        StartCoroutine(FlashColor());

        if(uiManager && uiManager.healthSlider) uiManager.healthSlider.value = currentHealth / maxHealth;
        if(currentHealth <= 0 && OnDeath != null) OnDeath.Invoke(); 
    }
    
    IEnumerator FlashColor()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr)
        {
            Color original = sr.color;
            sr.color = Color.red; 
            yield return new WaitForSeconds(0.1f);
            sr.color = original;  
        }
    }

    void PrepareLevelUp()
    {
        if (growthLevel >= maxGrowthLevel) return;

        List<UpgradeOption> validUpgrades = new List<UpgradeOption>();
        foreach(var upgrade in allUpgrades)
        {
            // --- STRICT LIMITS ---
            if (upgrade.type == UpgradeType.Hydra && hydraCount >= maxHydraCount) continue;
            if (upgrade.type == UpgradeType.Domain && domainCount >= maxDomainCount) continue;
            
            // --- NEW: TITAN LIMIT ---
            if (upgrade.type == UpgradeType.Titan && titanCount >= maxTitanCount) continue;
            // ------------------------

            if (upgrade.type == UpgradeType.MultiShot && isMultiShot) continue; 
            if (upgrade.type == UpgradeType.Fungal && isFungal) continue;
            
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

        // --- NEW: TRIGGER GROW ANIMATION ---
        if(animator != null) animator.SetTrigger("Grow");
        // -----------------------------------

        currentHealth = maxHealth; 
        AudioManager.instance.PlayGrowth();

        switch (type)
        {
            case UpgradeType.Hydra:
                hydraCount++;
                SpawnHydraHead(); 
                break;
            case UpgradeType.Vampiric:
                isVampiric = true;
                // THIS LINE ADDS THE STACKING EFFECT
                healOnKillAmount += 2f; // Adds +2 HP per Upgrade
                absorptionRadius *= 1.2f; 
                break;
            case UpgradeType.Titan:
                titanCount++; // Count usage
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
                whipDamage *= 1.2f;
                break;
            case UpgradeType.Fungal:
                isFungal = true;
                break;
            case UpgradeType.MultiShot:
                isMultiShot = true;
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

        float angleStep = 360f / activeHydraHeads.Count; 
        
        for (int i = 0; i < activeHydraHeads.Count; i++)
        {
            Transform headT = activeHydraHeads[i].transform;
            float angle = i * angleStep;
            
            // 1. POSITION
            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * hydraBaseDistance;
            float y = Mathf.Sin(angle * Mathf.Deg2Rad) * hydraBaseDistance;
            headT.localPosition = new Vector3(x, y, 0);

            // 2. ROTATION (This makes them face away)
            // We use the new 'hydraRotationOffset' variable here
            headT.localRotation = Quaternion.Euler(0, 0, angle + hydraRotationOffset);

            // 3. SCALE
            float currentScale = transform.localScale.x;
            float finalSize = (1f / currentScale) * hydraHeadScale; 
            headT.localScale = new Vector3(finalSize, finalSize, 1f);
        }
    }
    void UpdateWhipUI()
    {
        if (whipIcons == null) return;

        for (int i = 0; i < whipIcons.Length; i++)
        {
            if (whipIcons[i] == null) continue;

            // If current charges are greater than this index, it's Active
            if (i < currentWhipCharges)
            {
                whipIcons[i].color = iconActiveColor;
            }
            else
            {
                whipIcons[i].color = iconEmptyColor;
            }
        }
    }
}

// --- HYDRA CLASS (Keep at bottom) ---
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

        if (isDigesting)
        {
            digestionTimer -= Time.deltaTime; 
            if(sr) sr.color = new Color(0.6f, 0.4f, 0.2f); 
            if (digestionTimer <= 0)
            {
                isDigesting = false;
                if(owner != null) owner.HealFromDigestion(); 
                if(sr) sr.color = Color.white; 
            }
            return; 
        }

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