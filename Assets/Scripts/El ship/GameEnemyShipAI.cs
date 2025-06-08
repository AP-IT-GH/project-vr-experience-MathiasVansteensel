using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

public class GameEnemyShipAI : Agent, IHealthSystem, ICannonballEvents
{
    [Header("Ship Movement")]
    public float maxForwardSpeed = 15f;
    public float forwardForce = 50f;
    public float maxTurnSpeed = 60f;
    public float turnAcceleration = 120f;
    public float dragCoefficient = 2f;
    
    [Header("Cannons")]
    public Transform leftCannon;
    public Transform rightCannon;
    public float cannonAimMin = -10f;
    public float cannonAimMax = 30f;
    public float cannonAimSpeed = 30f;
    public GameObject cannonballPrefab;
    public Transform leftCannonMuzzle;
    public Transform rightCannonMuzzle;
    public float cannonFireForce = 1000f;
    public float reloadTime = 2f;
    
    [Header("AI Settings")]
    public Transform target;
    public bool aiEnabled = true;
    
    // Private fields
    private float currentTurnSpeed = 0f;
    private float leftCannonAngle = 0f;
    private float rightCannonAngle = 0f;
    private bool leftCannonReady = true;
    private bool rightCannonReady = true;
    private Rigidbody rb;
    
    // ML-Agents components - will be found/created automatically
    private DecisionRequester decisionRequester;
    private BehaviorParameters behaviorParameters;
    private Agent internalAgent;
    
    // IHealthSystem implementation
    public int maxHealth = 5;
    private int currentHealth;
    
    private void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        SetupMLAgents();
    }
    
    private void SetupMLAgents()
    {
        // Find or add DecisionRequester
        decisionRequester = GetComponent<DecisionRequester>();
        if (decisionRequester == null)
        {
            decisionRequester = gameObject.AddComponent<DecisionRequester>();
            decisionRequester.DecisionPeriod = 5;
            decisionRequester.TakeActionsBetweenDecisions = true;
        }
        
        // Find existing BehaviorParameters (don't create - let user add manually)
        behaviorParameters = GetComponent<BehaviorParameters>();
        if (behaviorParameters == null)
        {
            Debug.LogError("BehaviorParameters component not found! Please add it manually and assign your trained model.");
            return;
        }
        
        // Set behavior parameters for inference
        behaviorParameters.BehaviorType = BehaviorType.InferenceOnly;
    }
    
    public void ExecuteAction(ActionBuffers actions)
    {
        if (!aiEnabled) return;
        
        float speedInput = Mathf.Clamp01(actions.ContinuousActions[0]);
        float turnInput = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        float leftAimInput = Mathf.Clamp(actions.ContinuousActions[2], -1f, 1f);
        float rightAimInput = Mathf.Clamp(actions.ContinuousActions[3], -1f, 1f);
        bool fireLeft = actions.DiscreteActions[0] > 0;
        bool fireRight = actions.DiscreteActions[1] > 0;
        
        MoveForward(speedInput);
        TurnShip(turnInput);
        AimCannons(leftAimInput, rightAimInput);
        if (fireLeft) FireLeftCannon();
        if (fireRight) FireRightCannon();
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        // Ship orientation
        sensor.AddObservation(transform.forward);
        sensor.AddObservation(transform.right);
        
        // Agent's own velocity
        sensor.AddObservation(rb.linearVelocity);
        sensor.AddObservation(rb.angularVelocity.y);
        
        // Current forward speed (normalized)
        float currentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        sensor.AddObservation(currentSpeed / maxForwardSpeed);
        
        // Cannon angles (normalized)
        sensor.AddObservation(Mathf.InverseLerp(cannonAimMin, cannonAimMax, leftCannonAngle));
        sensor.AddObservation(Mathf.InverseLerp(cannonAimMin, cannonAimMax, rightCannonAngle));
        
        // Cannon readiness
        sensor.AddObservation(leftCannonReady ? 1f : 0f);
        sensor.AddObservation(rightCannonReady ? 1f : 0f);
        
        // Target observations
        if (target != null)
        {
            Vector3 toTarget = target.position - transform.position;
            sensor.AddObservation(toTarget);
            
            Vector3 localTargetPos = transform.InverseTransformPoint(target.position);
            sensor.AddObservation(localTargetPos);
            
            Vector3 toTargetFromLeft = target.position - leftCannonMuzzle.position;
            Vector3 toTargetFromRight = target.position - rightCannonMuzzle.position;
            
            float leftCannonAlignment = Vector3.Dot(leftCannon.forward, toTargetFromLeft.normalized);
            float rightCannonAlignment = Vector3.Dot(rightCannon.forward, toTargetFromRight.normalized);
            
            sensor.AddObservation(leftCannonAlignment);
            sensor.AddObservation(rightCannonAlignment);
            
            float targetSide = Vector3.Dot(transform.right, toTarget.normalized);
            sensor.AddObservation(targetSide);
        }
        else
        {
            for (int i = 0; i < 9; i++) sensor.AddObservation(0f);
        }
        
        // Combat range observations
        if (target != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            float maxCombatRange = 50f;
            
            sensor.AddObservation(Mathf.Clamp01(distanceToTarget / maxCombatRange));
            sensor.AddObservation(distanceToTarget <= maxCombatRange ? 1f : 0f);
            
            Vector3 toTarget = (target.position - transform.position).normalized;
            float approachEfficiency = Vector3.Dot(transform.forward, toTarget);
            sensor.AddObservation(approachEfficiency);
        }
        else
        {
            for (int i = 0; i < 3; i++) sensor.AddObservation(0f);
        }
    }
    
    private void MoveForward(float speedInput)
    {
        Vector3 forwardForceVector = transform.forward * (speedInput * forwardForce);
        rb.AddForce(forwardForceVector);
        
        Vector3 drag = -rb.linearVelocity * dragCoefficient;
        rb.AddForce(drag);
        
        float currentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        if (currentSpeed > maxForwardSpeed)
        {
            Vector3 excessVelocity = transform.forward * (currentSpeed - maxForwardSpeed);
            rb.linearVelocity -= excessVelocity;
        }
        
        if (currentSpeed < 0)
        {
            Vector3 backwardVelocity = transform.forward * currentSpeed;
            rb.linearVelocity -= backwardVelocity;
        }
    }
    
    private void TurnShip(float turnInput)
    {
        float targetTurnSpeed = turnInput * maxTurnSpeed;
        currentTurnSpeed = Mathf.MoveTowards(currentTurnSpeed, targetTurnSpeed, turnAcceleration * Time.deltaTime);
        rb.angularVelocity = new Vector3(0f, currentTurnSpeed * Mathf.Deg2Rad, 0f);
    }
    
    private void AimCannons(float leftAimInput, float rightAimInput)
    {
        leftCannonAngle = Mathf.Clamp(leftCannonAngle + leftAimInput * cannonAimSpeed * Time.deltaTime, cannonAimMin, cannonAimMax);
        rightCannonAngle = Mathf.Clamp(rightCannonAngle + rightAimInput * cannonAimSpeed * Time.deltaTime, cannonAimMin, cannonAimMax);
        leftCannon.localRotation = Quaternion.Euler(leftCannonAngle, 0, 0);
        rightCannon.localRotation = Quaternion.Euler(rightCannonAngle, 0, 0);
    }
    
    private void FireLeftCannon()
    {
        if (leftCannonReady)
        {
            StartCoroutine(FireCannon(leftCannonMuzzle, "left"));
        }
    }
    
    private void FireRightCannon()
    {
        if (rightCannonReady)
        {
            StartCoroutine(FireCannon(rightCannonMuzzle, "right"));
        }
    }
    
    private System.Collections.IEnumerator FireCannon(Transform muzzle, string cannon)
    {
        if (cannon == "left")
            leftCannonReady = false;
        else if (cannon == "right")
            rightCannonReady = false;
        
        GameObject cannonball = Instantiate(cannonballPrefab, muzzle.position, muzzle.rotation);
        var cannonballScript = cannonball.GetComponent<Cannonball>();
        if (cannonballScript != null)
        {
            cannonballScript.shooter = this.gameObject; // Pass the game object instead
            cannonballScript.target = target;
        }
        
        if (cannonball.TryGetComponent<Rigidbody>(out var rbCannon))
        {
            rbCannon.linearVelocity = rb.linearVelocity;
            rbCannon.AddForce(muzzle.forward * cannonFireForce, ForceMode.Impulse);
        }
        
        yield return new WaitForSeconds(reloadTime);
        
        if (cannon == "left")
            leftCannonReady = true;
        else if (cannon == "right")
            rightCannonReady = true;
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    public void SetAIEnabled(bool enabled)
    {
        aiEnabled = enabled;
        if (decisionRequester != null)
            decisionRequester.enabled = enabled;
        if (behaviorParameters != null)
            behaviorParameters.enabled = enabled;
    }
    
    // IHealthSystem implementation
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"AI Ship {name} took {damage} damage! Health: {currentHealth}");
        
        if (currentHealth <= 0)
        {
            Debug.Log($"AI Ship {name} destroyed!");
            // Handle AI death
            SetAIEnabled(false);
            // Maybe respawn after delay, award points to player, etc.
        }
    }
    
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public bool IsAlive() => currentHealth > 0;
    
    // ICannonballEvents implementation
    public void OnCannonballHit(bool hitIntendedTarget)
    {
        Debug.Log($"AI {name} scored a hit!");
        // Add game logic - score points, play sounds, etc.
    }
    
    public void OnCannonballMiss()
    {
        Debug.Log($"AI {name} missed");
        // Track accuracy, play miss sounds, etc.
    }
    
    public override void OnActionReceived(ActionBuffers actions)
    {
        ExecuteAction(actions); // Call your existing method
    }

    public override void OnEpisodeBegin() { } // Empty for inference
    public override void Heuristic(in ActionBuffers actionsOut) { } // Empty for inference
}