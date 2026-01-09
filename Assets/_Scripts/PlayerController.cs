using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI; // REQUIRED FOR UI IMAGES
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
        public Sprite cardSprite; // <--- NEW: Drag your Card Image here in Inspector!
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
    public Image[] whipIcons; 
    public Color iconActiveColor = Color.green;
    public Color iconEmptyColor = new Color(0.5f, 0.5f, 0.5f, 0.3f); 

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
    // --- HYDRA CONTROLS ---
    public float hydraBaseDistance = 0.5f; // Smaller number = Closer to body
    public float hydraHeadScale = 1.0f;    // Bigger number = Bigger heads
    public float hydraRotationOffset = -90f; // Adjust to face heads outward
    // ----------------------
    private List<HydraTurret> activeHydraHeads = new List<HydraTurret>();

    [Header("Mutation: Vampiric")]
    public bool isVampiric = false;
    public float healOnKillAmount = 0f; 
    private float nextVampireHealTime = 0f;

    [Header("XP System")]
    public float currentXP = 0f;
    public float xpToNextLevel = 30f; 
    
    [Header("Whip Stats")]
    public float whipRange = 3.5f;
    public float whipDamage = 50f;

    [Header("Defense")]
    public float invincibilityDuration = 0.5f; 
    private float nextDamageTime = 0f;         

    [Header("Upgrade Counts (Strict Limits)")]
    public int hydraCount = 0;
    public int maxHydraCount = 4; 
    
    public int domainCount = 0;
    public int maxDomainCount = 2; 

    public int titanCount = 0;
    public int maxTitanCount = 2; 

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

    // --- NEW: PUBLIC LIST FOR INSPECTOR SETUP ---
    [Header("Upgrade Configuration")]
    public List<UpgradeOption> allUpgrades; 
    // --------------------------------------------

    void Start()
    {
        mainCam = Camera.main;
        currentWhipCharges = maxWhipCharges;
        UpdateWhipUI(); 
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
                UpdateWhipUI(); 
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
        if (Input.GetKeyDown(KeyCode.A) && Time.time >= nextFireTime)
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
                    UpdateWhipUI(); 
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
            // Use shootPoint.rotation
            CreateBullet(shootPoint.position, shootPoint.rotation, dmg, homing);
            
            if (shootPointLeft) 
                CreateBullet(shootPointLeft.position, shootPointLeft.rotation, dmg, homing);
            
            if (shootPointRight) 
                CreateBullet(shootPointRight.position, shootPointRight.rotation, dmg, homing);
        }
        else
        {
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
        if(animator != null) animator.SetTrigger("Whip");

        // Spawn VFX fixed (No rotation)
       if (whipVFXPrefab != null)
        {
            GameObject vfx = Instantiate(whipVFXPrefab, transform.position, Quaternion.identity);
            
            // Scale based on Domain Expansion
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
        
        // Flipped 180 degrees (+90)
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
        if (growthLevel >= maxGrowthLevel) 
        {
            if(uiManager) uiManager.ShowWinScreen();
            return;
        }

        List<UpgradeOption> validUpgrades = new List<UpgradeOption>();
        foreach(var upgrade in allUpgrades)
        {
            // Strict Limits
            if (upgrade.type == UpgradeType.Hydra && hydraCount >= maxHydraCount) continue;
            if (upgrade.type == UpgradeType.Domain && domainCount >= maxDomainCount) continue;
            if (upgrade.type == UpgradeType.Titan && titanCount >= maxTitanCount) continue;
            if (upgrade.type == UpgradeType.MultiShot && isMultiShot) continue; 
            if (upgrade.type == UpgradeType.Fungal && isFungal) continue;
            
            validUpgrades.Add(upgrade);
        }

        // Shuffle and Pick 3
        List<UpgradeOption> shuffled = validUpgrades.OrderBy(x => System.Guid.NewGuid()).ToList();
        int optionsToTake = Mathf.Min(3, shuffled.Count);
        currentRoundOptions = shuffled.Take(optionsToTake).ToList();

        // --- NEW: CALL THE CARD SYSTEM INSTEAD OF TEXT ---
        if(uiManager) uiManager.ShowUpgradeCards(currentRoundOptions, OnUpgradeSelected);
        // -------------------------------------------------
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

        if(animator != null) animator.SetTrigger("Grow");

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
                healOnKillAmount += 2f; // Stackable
                absorptionRadius *= 1.2f; 
                break;
            case UpgradeType.Titan:
                titanCount++; 
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
        float currentScale = transform.localScale.x; 

        for (int i = 0; i < activeHydraHeads.Count; i++)
        {
            Transform headT = activeHydraHeads[i].transform;
            float angle = i * angleStep;
            
            // --- FIX 1: REVERT POSITION LOGIC ---
            // We use 'hydraBaseDistance' directly as the Local Offset.
            // As the Parent scales up, this Local Offset scales with it naturally.
            float distance = hydraBaseDistance; 
            
            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * distance;
            float y = Mathf.Sin(angle * Mathf.Deg2Rad) * distance;
            headT.localPosition = new Vector3(x, y, 0);

            // --- FIX 2: ROTATION ---
            headT.localRotation = Quaternion.Euler(0, 0, angle + hydraRotationOffset);

            // --- FIX 3: KEEP HEAD SIZE NORMAL ---
            // This prevents the heads from becoming giant pixels when you grow
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

            if (i < currentWhipCharges) whipIcons[i].color = iconActiveColor;
            else whipIcons[i].color = iconEmptyColor;
        }
    }
}

// --- HYDRA CLASS ---
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