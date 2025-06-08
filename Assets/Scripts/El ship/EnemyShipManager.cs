using UnityEngine;

public class EnemyShipManager : MonoBehaviour
{
    [Header("Enemy Ship Settings")]
    [SerializeField] private GameObject enemyShipPrefab; // Assign enemy ship prefab in inspector
    [SerializeField] private Transform spawnPoint; // Where to spawn enemy ships
    [SerializeField] private Transform targetTransform; // Target for enemy ships to follow/attack
    
    [Header("Spawn Settings")]
    [SerializeField] private float spawnDelay = 2f; // Delay before spawning new ship after destruction
    [SerializeField] private bool autoSpawn = true; // Whether to automatically spawn ships
    
    private GameObject currentEnemyShip;
    private float spawnTimer = 0f;
    private bool needsToSpawn = true;

    void Start()
    {
        if (autoSpawn && needsToSpawn)
        {
            SpawnEnemyShip();
        }
    }

    void Update()
    {
        // Check if current enemy ship is destroyed
        if (currentEnemyShip == null && autoSpawn)
        {
            spawnTimer += Time.deltaTime;
            
            if (spawnTimer >= spawnDelay)
            {
                SpawnEnemyShip();
                spawnTimer = 0f;
            }
        }
    }

    private void SpawnEnemyShip()
    {
        if (enemyShipPrefab == null)
        {
            Debug.LogError("Enemy ship prefab is not assigned!");
            return;
        }

        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        currentEnemyShip = Instantiate(enemyShipPrefab, spawnPosition, spawnRotation);
        
        // Set the target for the enemy ship if it has a component that needs it
        SetEnemyShipTarget(currentEnemyShip);
        
        Debug.Log("Enemy ship spawned at: " + spawnPosition);
        needsToSpawn = false;
    }

    private void SetEnemyShipTarget(GameObject enemyShip)
    {
        if (targetTransform == null) return;

        // Try to find common enemy ship AI components and set their target
        var enemyAI = enemyShip.GetComponent<GameEnemyShipAI>();
        if (enemyAI != null)
        {
            enemyAI.SetTarget(targetTransform);
            return;
        }

        // Add more component types as needed
        Debug.LogWarning("No compatible target-setting component found on enemy ship");
    }

    // Public methods for manual control
    public void ForceSpawnEnemyShip()
    {
        if (currentEnemyShip != null)
        {
            Destroy(currentEnemyShip);
        }
        SpawnEnemyShip();
    }

    public void DestroyCurrentEnemyShip()
    {
        if (currentEnemyShip != null)
        {
            Destroy(currentEnemyShip);
            currentEnemyShip = null;
        }
    }

    public void SetTarget(Transform newTarget)
    {
        targetTransform = newTarget;
        
        // Update current enemy ship target if one exists
        if (currentEnemyShip != null)
        {
            SetEnemyShipTarget(currentEnemyShip);
        }
    }

    public void SetAutoSpawn(bool enabled)
    {
        autoSpawn = enabled;
    }
}
