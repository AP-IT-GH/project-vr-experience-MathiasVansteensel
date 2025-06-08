using UnityEngine;

public class Cannonball : MonoBehaviour
{
    [Header("Cannonball Settings")]
    public float lifetime = 10f;
    public int damage = 1;
    public GameObject hitEffect; // Optional explosion/impact effect
    public LayerMask targetLayers = -1; // What can this cannonball hit
    
    [Header("Audio")]
    public AudioClip hitSound;
    public AudioClip missSound;
    
    // References set by the firing ship
    [HideInInspector] public GameObject shooter; // The ship that fired this
    [HideInInspector] public Transform target;   // The intended target
    
    private Rigidbody rb;
    private bool hasHit = false;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Destroy cannonball after lifetime expires
        Destroy(gameObject, lifetime);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return; // Prevent multiple hits
        
        // Don't hit the shooter
        if (other.gameObject == shooter) return;
        
        // Check if we hit something we can damage
        if (((1 << other.gameObject.layer) & targetLayers) != 0)
        {
            HandleHit(other);
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return; // Prevent multiple hits
        
        // Don't hit the shooter
        if (collision.gameObject == shooter) return;
        
        // Check if we hit something we can damage
        if (((1 << collision.gameObject.layer) & targetLayers) != 0)
        {
            HandleHit(collision.collider);
        }
        else
        {
            // Hit something else (terrain, obstacle, etc.)
            HandleMiss(collision.contacts[0].point);
        }
    }
    
    private void HandleHit(Collider hitTarget)
    {
        hasHit = true;
        
        // Check if we hit our intended target
        bool hitIntendedTarget = (target != null && hitTarget.transform == target);
        
        // Apply damage to the target
        var targetHealth = hitTarget.GetComponent<IHealthSystem>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage);
        }
        
        // Try different health systems
        var shipHealth = hitTarget.GetComponent<ShipHealth>();
        if (shipHealth != null)
        {
            shipHealth.TakeDamage(damage);
        }
        
        // Notify the shooter about the hit
        NotifyShooterOfHit(hitIntendedTarget);
        
        // Create hit effects
        CreateHitEffect(hitTarget.transform.position);
        
        // Play hit sound
        PlaySound(hitSound);
        
        // Log the hit
        Debug.Log($"Cannonball hit {hitTarget.name}! Damage: {damage}");
        
        // Destroy the cannonball
        Destroy(gameObject);
    }
    
    private void HandleMiss(Vector3 hitPoint)
    {
        hasHit = true;
        
        // Notify shooter of miss
        NotifyShooterOfMiss();
        
        // Create miss effect (splash, dust, etc.)
        CreateMissEffect(hitPoint);
        
        // Play miss sound
        PlaySound(missSound);
        
        // Destroy the cannonball
        Destroy(gameObject);
    }
    
    private void NotifyShooterOfHit(bool hitIntendedTarget)
    {
        if (shooter == null) return;
        
        // Try to notify GameEnemyShipAI
        var gameAI = shooter.GetComponent<GameEnemyShipAI>();
        if (gameAI != null)
        {
            OnGameAIHit(gameAI, hitIntendedTarget);
            return;
        }
        
        // Try to notify training agent (for compatibility)
        var trainingAgent = shooter.GetComponent<EnemyShipAgent>();
        if (trainingAgent != null)
        {
            trainingAgent.OnHitOpponent();
            return;
        }
        
        // Try to notify any component with OnCannonballHit method
        var hitNotifier = shooter.GetComponent<ICannonballEvents>();
        if (hitNotifier != null)
        {
            hitNotifier.OnCannonballHit(hitIntendedTarget);
        }
    }
    
    private void NotifyShooterOfMiss()
    {
        if (shooter == null) return;
        
        // Try to notify GameEnemyShipAI
        var gameAI = shooter.GetComponent<GameEnemyShipAI>();
        if (gameAI != null)
        {
            OnGameAIMiss(gameAI);
            return;
        }
        
        // Try to notify training agent (for compatibility)
        var trainingAgent = shooter.GetComponent<EnemyShipAgent>();
        if (trainingAgent != null)
        {
            trainingAgent.OnMissedShot();
            return;
        }
        
        // Try to notify any component with OnCannonballMiss method
        var missNotifier = shooter.GetComponent<ICannonballEvents>();
        if (missNotifier != null)
        {
            missNotifier.OnCannonballMiss();
        }
    }
    
    private void OnGameAIHit(GameEnemyShipAI gameAI, bool hitIntendedTarget)
    {
        // Handle hit for game AI
        Debug.Log($"Game AI {gameAI.name} scored a hit!");
        
        // You can add game-specific logic here:
        // - Update score
        // - Trigger achievements
        // - Update UI
        // - etc.
        
        if (hitIntendedTarget)
        {
            // Extra logic for hitting the intended target
            Debug.Log("Hit the intended target!");
        }
    }
    
    private void OnGameAIMiss(GameEnemyShipAI gameAI)
    {
        // Handle miss for game AI
        Debug.Log($"Game AI {gameAI.name} missed a shot");
        
        // You can add game-specific logic here:
        // - Update accuracy stats
        // - Trigger miss effects
        // - etc.
    }
    
    private void CreateHitEffect(Vector3 position)
    {
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, position, Quaternion.identity);
            
            // Auto-destroy effect after a few seconds
            var particleSystem = effect.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                Destroy(effect, particleSystem.main.duration + particleSystem.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(effect, 3f); // Default 3 seconds
            }
        }
    }
    
    private void CreateMissEffect(Vector3 position)
    {
        // You can create different effects for misses (water splash, dirt cloud, etc.)
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, position, Quaternion.identity);
            Destroy(effect, 3f);
        }
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }
    }
    
    // Optional: Show trajectory in Scene view for debugging
    private void OnDrawGizmos()
    {
        if (rb != null)
        {
            // Draw velocity vector
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * 2f);
            
            // Draw line to target if we have one
            if (target != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, target.position);
            }
        }
    }
}