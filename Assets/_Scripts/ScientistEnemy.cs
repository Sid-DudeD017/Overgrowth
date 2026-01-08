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

    // Track the most recent gas bubble/cloud for cleanup
    private GameObject activeGasCloud; 

    [Header("Gas System")]
    public GameObject gasCloudPrefab; 

    [Header("Audio")]
    public AudioSource enemyAudio;
    public AudioClip valveTurnSound;
    public AudioClip gasHissSound;

    protected override void Update()
    {
        // 1. ZOMBIE CHECK (Passive Mode)
        // If infected, we simply stop doing ANYTHING. 
        // We rely on EnemyBase to handle the dying timer and spreading.
        if (isInfected)
        {
            return;        
        }

        if (isGassing) return;

        // 2. VALVE LOGIC
        if (currentTargetValve == null)
        {
            // If I don't have a job, look for one...
            if (Time.time >= nextSearchTime)
            {
                FindClosestValve();
                nextSearchTime = Time.time + 1.0f; 
            }
            
            // --- COWARD BEHAVIOR (Only if HEALTHY) ---
            // If I am healthy but have no job, run away from the player.
            if (currentTargetValve == null && target != null)
            {
                 Vector2 fleeDir = transform.position - target.position;
                 transform.position += (Vector3)fleeDir.normalized * speed * Time.deltaTime;
            }
            // ---------------------------------------
            return;
        }

        // 3. MOVE TO VALVE (If we have one)
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

        // Infinite Loop (runs until Scientist dies or gets infected)
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
                
                // Track this specific cloud so we can delete it if we get infected immediately
                activeGasCloud = Instantiate(gasCloudPrefab, currentTargetValve.transform.position, randomSpray);
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

        // 2. Destroy the Gas Cloud
        if (activeGasCloud != null)
        {
            Destroy(activeGasCloud);
        }
    }

    // --- NEW: STOP WORKING WHEN INFECTED ---
    protected override void OnInfected()
    {
        // 1. Stop the gas timer immediately (Stops the While Loop)
        StopAllCoroutines(); 

        // 2. Reset the Valve Visuals (if we were turning one)
        if (currentTargetValve != null)
        {
            SpriteRenderer valveSR = currentTargetValve.GetComponent<SpriteRenderer>();
            if (valveSR != null) valveSR.color = Color.white; // Reset to normal
        }

        // 3. Destroy any gas we just created (The one currently in the air)
        if (activeGasCloud != null)
        {
            Destroy(activeGasCloud);
        }

        // 4. Reset Logic Flags
        isGassing = false;
        currentTargetValve = null;
        
        // Note: We do NOT need to handle death/spreading here. 
        // EnemyBase handles the spread limit (4 people) and the death timer automatically.
        Debug.Log("Scientist infected! Abandoning valve.");
    }
}