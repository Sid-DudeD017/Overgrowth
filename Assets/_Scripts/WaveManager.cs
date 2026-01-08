using UnityEngine;
using System.Collections;
using TMPro; 

public class WaveManager : MonoBehaviour
{
    [System.Serializable]
    public class Wave
    {
        public string waveName = "Wave";
        public int enemyCount = 10;
        public float spawnRate = 1.0f;
    }

    [Header("Configuration")]
    public Wave[] tutorialWaves; 
    public TextMeshProUGUI waveText; 

    // --- FIX 1: ADD THESE MISSING VARIABLES ---
    [Header("Spawning Area")]
    public float spawnRadius = 15f; // Distance from center
    public Transform centerPoint;   // Drag Player here

    [Header("Endless Scaling")]
    public int baseEnemyCount = 15;
    public float baseSpawnRate = 2.0f;

    [Header("Prefabs")]
    public GameObject scientistPrefab;
    public GameObject guardPrefab;
    public int maxActiveScientists = 2; 

    private int currentWaveIndex = 0;
    private bool stopSpawning = false;

    void Start()
    {
        StartCoroutine(StartNextWave());
    }

    IEnumerator StartNextWave()
    {
        Wave currentWave = new Wave();

        // 1. Determine Wave Data
        if (currentWaveIndex < tutorialWaves.Length)
        {
            currentWave = tutorialWaves[currentWaveIndex];
        }
        else
        {
            // ENDLESS MATH
            int infiniteIndex = currentWaveIndex - tutorialWaves.Length + 1;
            currentWave.waveName = "Wave " + (currentWaveIndex + 1);
            currentWave.enemyCount = Mathf.RoundToInt(baseEnemyCount + (infiniteIndex * 5)); 
            currentWave.spawnRate = baseSpawnRate + (infiniteIndex * 0.2f);
            
            if(currentWave.spawnRate > 8f) currentWave.spawnRate = 8f;
        }

        if(waveText) waveText.text = currentWave.waveName;
        
        // 3. Spawning Loop
        for (int i = 0; i < currentWave.enemyCount; i++)
        {
            while (Time.timeScale == 0) yield return null; 
            if (stopSpawning) yield break;

            SpawnEnemy();
            yield return new WaitForSeconds(1f / currentWave.spawnRate);
        }

        yield return new WaitForSeconds(5.0f); 
        currentWaveIndex++;
        StartCoroutine(StartNextWave());
    }

    void SpawnEnemy()
    {
        // --- FIX 1: CORRECT CIRCLE MATH ---
        // Get a point on the edge of the circle (normalized * radius)
        Vector2 randomCirclePoint = Random.insideUnitCircle.normalized * spawnRadius;
        
        Vector3 spawnPos = Vector3.zero;
        if (centerPoint != null) 
        {
            spawnPos = centerPoint.position + (Vector3)randomCirclePoint;
        }
        else 
        {
            spawnPos = (Vector3)randomCirclePoint;
        }

        GameObject enemyToSpawn = guardPrefab; 
        int currentScientists = FindObjectsByType<ScientistEnemy>(FindObjectsSortMode.None).Length;
        
        if (currentScientists < maxActiveScientists && Random.Range(0, 100) < 30) 
        {
            enemyToSpawn = scientistPrefab;
        }

        Instantiate(enemyToSpawn, spawnPos, Quaternion.identity);
    }

    public void StopGame()
    {
        stopSpawning = true;
    }
}