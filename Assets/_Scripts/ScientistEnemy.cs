using UnityEngine;
using System.Collections;

public class ScientistEnemy : EnemyBase
{
    [Header("Scientist AI")]
    public float searchRange = 20f;
    public float interactionTime = 3.0f; 
    public Color alertColor = Color.red; 
    
    private GameObject currentTargetValve;
    private bool isGassing = false;
    private float nextSearchTime = 0f;

    // NEW: We track the specific cloud we created
    private GameObject activeGasCloud; 

    [Header("Gas System")]
    public GameObject gasCloudPrefab; 

    [Header("Audio")]
    public AudioSource enemyAudio;
    public AudioClip valveTurnSound;
    public AudioClip gasHissSound;

    protected override void Update()
    {
        if (isGassing) return;

        if (currentTargetValve == null)
        {
            if (Time.time >= nextSearchTime)
            {
                FindClosestValve();
                nextSearchTime = Time.time + 1.0f; 
            }
            return;
        }

        MoveToValve();
    }

    void FindClosestValve()
    {
        GameObject[] valves = GameObject.FindGameObjectsWithTag("Valve");
        float closestDist = Mathf.Infinity;
        GameObject bestValve = null;

        foreach (GameObject v in valves)
        {
            float d = Vector2.Distance(transform.position, v.transform.position);
            if (d < closestDist && d < searchRange)
            {
                closestDist = d;
                bestValve = v;
            }
        }
        currentTargetValve = bestValve;
    }

    void MoveToValve()
    {
        if (currentTargetValve == null) return;

        float dist = Vector2.Distance(transform.position, currentTargetValve.transform.position);
        
        if (dist > 0.5f)
        {
            transform.position = Vector2.MoveTowards(transform.position, currentTargetValve.transform.position, speed * Time.deltaTime);
        }
        else
        {
            StartCoroutine(ReleaseGas());
        }
    }

   IEnumerator ReleaseGas()
    {
        isGassing = true;
        SpriteRenderer valveSR = currentTargetValve.GetComponent<SpriteRenderer>();

        // STAGE 1: TURNING (Yellow Warning)
        if(valveSR != null) valveSR.color = Color.yellow; 
        if(enemyAudio && valveTurnSound) enemyAudio.PlayOneShot(valveTurnSound);

        yield return new WaitForSeconds(interactionTime); 
        
        // STAGE 2: SPRAY STREAM (Infinite)
        if(valveSR != null) valveSR.color = alertColor; // Turn Red

        if(enemyAudio && gasHissSound)
        {
            enemyAudio.clip = gasHissSound;
            enemyAudio.loop = true;
            enemyAudio.Play();
        }

        float delayBetweenBubbles = 0.2f; // Adjust firing speed here

        // FIX: Infinite Loop (runs until Scientist dies)
        while (true)
        {
            // 1. Aim at Player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Quaternion sprayRotation = Quaternion.identity;

            if (player != null)
            {
                Vector3 dir = player.transform.position - currentTargetValve.transform.position;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                sprayRotation = Quaternion.Euler(0, 0, angle);
            }

            // 2. Spawn Bubble
            if (gasCloudPrefab != null)
            {
                // Add randomness for "Spray" effect
                Quaternion randomSpray = sprayRotation * Quaternion.Euler(0, 0, Random.Range(-15f, 15f));
                Instantiate(gasCloudPrefab, currentTargetValve.transform.position, randomSpray);
            }

            // 3. Wait before next shot
            yield return new WaitForSeconds(delayBetweenBubbles);
        }
    }
    // --- FIX: CLEAN UP GAS ON DEATH ---
    void OnDestroy()
    {
        // 1. Reset Valve Color
        if (currentTargetValve != null)
        {
            SpriteRenderer valveSR = currentTargetValve.GetComponent<SpriteRenderer>();
            if (valveSR != null) valveSR.color = Color.white;
        }

        // 2. Destroy the Gas Cloud (The Fix)
        if (activeGasCloud != null)
        {
            Destroy(activeGasCloud);
        }
    }
}