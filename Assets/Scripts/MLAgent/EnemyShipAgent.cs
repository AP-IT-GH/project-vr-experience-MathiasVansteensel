using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class EnemyShipAgent : Agent
{
    [Header("Ship Movement")]
    public float maxForwardSpeed = 15f; // Maximum forward speed
    public float forwardForce = 50f; // Force applied for acceleration
    public float maxTurnSpeed = 60f; // degrees per second
    public float turnAcceleration = 120f; // degrees per second squared
    public float dragCoefficient = 2f; // Air/water resistance

    [Header("Cannons")]
    public Transform leftCannon;
    public Transform rightCannon;
    public float cannonAimMin = -10f;
    public float cannonAimMax = 30f;
    public float cannonAimSpeed = 30f; // degrees per second
    public GameObject cannonballPrefab;
    public Transform leftCannonMuzzle;
    public Transform rightCannonMuzzle;
    public float cannonFireForce = 1000f;
    public float reloadTime = 2f;

    public Transform target; // Assign this in the inspector

    private float currentTurnSpeed = 0f;
    private float leftCannonAngle = 0f;
    private float rightCannonAngle = 0f;
    private bool leftCannonReady = true;
    private bool rightCannonReady = true;
    private Rigidbody rb;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private Vector3 previousTargetPos;
    private Vector3 previousAgentPos;

    private int timesHit = 0;
    private float episodeStartTime = 0f;

    // Add these private fields to track collision state
    private bool isCollidingWithObstacle = false;
    private float obstacleCollisionPenalty = -0.5f; // Increased from -0.1f

    // Add these private fields to track standing still
    private float stationaryTime = 0f;
    private float stationaryThreshold = 0.5f; // Speed below this is considered stationary
    private float maxStationaryTime = 10f; // End episode after 10 seconds
    private float stationaryPenalty = -0.005f; // Per-frame penalty for being stationary

    protected override void Awake()
    {
        rb = GetComponent<Rigidbody>();
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
    }

    public override void OnEpisodeBegin()
    {
        // Get the EnvironmentManager from this agent's parent/sibling
        var environmentManager = GetComponentInParent<EnvironmentManager>();
        if (environmentManager == null)
            environmentManager = transform.parent.GetComponentInChildren<EnvironmentManager>();

        // Any agent can trigger environment reset (removed first agent check)
        if (environmentManager != null)
        {
            environmentManager.ResetEnvironment();
        }

        // Reset agent state
        currentTurnSpeed = 0f;
        leftCannonAngle = 0f;
        rightCannonAngle = 0f;
        leftCannon.localRotation = Quaternion.Euler(leftCannonAngle, 0, 0);
        rightCannon.localRotation = Quaternion.Euler(rightCannonAngle, 0, 0);
        leftCannonReady = true;
        rightCannonReady = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        timesHit = 0;
        episodeStartTime = Time.time;
        transform.localPosition = initialPosition;
        transform.localRotation = initialRotation;

        // Reset collision state
        isCollidingWithObstacle = false;

        // Reset stationary timer
        stationaryTime = 0f;

        // Find all active agents with the same immediate parent (environment instance)
        var environment = transform.parent;
        if (environment == null)
        {
            Debug.LogError($"Agent {name} has no parent environment!");
            target = null;
            return;
        }

        var allAgents = environment.GetComponentsInChildren<EnemyShipAgent>(includeInactive: false);

        // Filter out this agent
        var otherAgents = System.Array.FindAll(allAgents, agent => agent != this && agent.gameObject.activeSelf);

        // Assign a random other agent as the target with safety check
        if (otherAgents.Length > 0)
        {
            target = otherAgents[Random.Range(0, otherAgents.Length)].transform;
        }
        else
        {
            target = null; // No valid targets
            Debug.LogWarning($"Agent {name} found no valid targets in environment {environment.name}");
        }

        previousTargetPos = target != null ? target.position : Vector3.zero;
        previousAgentPos = transform.position;
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

        // Target relative position
        if (target != null)
        {
            Vector3 toTarget = target.position - transform.position;
            
            // Global target position (current)
            sensor.AddObservation(toTarget);
            
            // LOCAL target position relative to ship orientation (CRITICAL for side cannons)
            Vector3 localTargetPos = transform.InverseTransformPoint(target.position);
            sensor.AddObservation(localTargetPos);
            
            // Target's position relative to each cannon's firing line
            Vector3 toTargetFromLeft = target.position - leftCannonMuzzle.position;
            Vector3 toTargetFromRight = target.position - rightCannonMuzzle.position;
            
            float leftCannonAlignment = Vector3.Dot(leftCannon.forward, toTargetFromLeft.normalized);
            float rightCannonAlignment = Vector3.Dot(rightCannon.forward, toTargetFromRight.normalized);
            
            sensor.AddObservation(leftCannonAlignment);
            sensor.AddObservation(rightCannonAlignment);
            
            // Whether target is on left or right side of ship
            float targetSide = Vector3.Dot(transform.right, toTarget.normalized); // -1 = left, +1 = right
            sensor.AddObservation(targetSide);
        }
        else
        {
            // Add zeros when no target (should be 9 to match target observations)
            for (int i = 0; i < 9; i++) sensor.AddObservation(0f);
        }

        // Add target distance and combat range observations
        if (target != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            float maxCombatRange = 50f;
            
            // Normalized distance (1.0 = at max range, 0.0 = very close)
            sensor.AddObservation(Mathf.Clamp01(distanceToTarget / maxCombatRange));
            
            // Binary: in combat range or not
            sensor.AddObservation(distanceToTarget <= maxCombatRange ? 1f : 0f);
            
            // Approach direction efficiency
            Vector3 toTarget = (target.position - transform.position).normalized;
            float approachEfficiency = Vector3.Dot(transform.forward, toTarget);
            sensor.AddObservation(approachEfficiency);
        }
        else
        {
            // Add zeros when no target
            for (int i = 0; i < 3; i++) sensor.AddObservation(0f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float speedInput = Mathf.Clamp01(actions.ContinuousActions[0]); // 0-1 for speed (no reverse)
        float turnInput = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f); // -1 to 1 for turning
        float leftAimInput = Mathf.Clamp(actions.ContinuousActions[2], -1f, 1f);
        float rightAimInput = Mathf.Clamp(actions.ContinuousActions[3], -1f, 1f);
        bool fireLeft = actions.DiscreteActions[0] > 0;
        bool fireRight = actions.DiscreteActions[1] > 0;

        MoveForward(speedInput);
        TurnShip(turnInput);
        AimCannons(leftAimInput, rightAimInput);
        if (fireLeft) FireLeftCannon();
        if (fireRight) FireRightCannon();

        // Check if agent is stationary
        float currentSpeed = rb.linearVelocity.magnitude;
        if (currentSpeed < stationaryThreshold)
        {
            stationaryTime += Time.deltaTime;
            AddReward(stationaryPenalty); // Apply penalty for standing still
            
            // End episode if stationary for too long
            if (stationaryTime >= maxStationaryTime)
            {
                AddReward(-1.0f); // Large penalty for timing out while stationary
                Debug.Log($"Agent {name} ended episode - stationary for {stationaryTime:F1} seconds");
                EndEpisode();
                return;
            }
        }
        else
        {
            stationaryTime = 0f; // Reset timer when moving
        }

        // Increased step penalty to encourage faster action
        AddReward(-0.001f); // Increased from -0.0001f (10x larger)

        // Continuous penalty for being in contact with obstacles
        if (isCollidingWithObstacle)
        {
            AddReward(obstacleCollisionPenalty);
        }

        
        // Reward shaping: predictive aiming
        if (target != null)
        {
            float aimingRewardMultiplier = Academy.Instance.EnvironmentParameters.GetWithDefault("aiming_reward", 1.0f);

            if (CalculatePredictiveAiming(out Vector3 predictedTargetPos, out Vector3 cannonballImpactPos))
            {
                float aimingError = Vector3.Distance(predictedTargetPos, cannonballImpactPos);

                // Much tighter accuracy requirements
                float maxAcceptableError = 5f; // Reduced from 10f
                float perfectAimError = 2f;
                
                if (aimingError < maxAcceptableError)
                {
                    float aimingAccuracy = 1f - (aimingError / maxAcceptableError);
                    AddReward(aimingAccuracy * 0.02f * aimingRewardMultiplier); // Much smaller base reward
                }

                // Only reward firing if aim is very good
                bool isFiring = actions.DiscreteActions[0] > 0 || actions.DiscreteActions[1] > 0;
                if (isFiring && aimingError < perfectAimError)
                {
                    float firingBonus = (1f - (aimingError / perfectAimError)) * 0.05f;
                    AddReward(firingBonus * aimingRewardMultiplier);
                }
                
                // Penalty for firing with poor aim
                if (isFiring && aimingError > maxAcceptableError)
                {
                    AddReward(-0.1f); // Penalty for wasted shots
                }
            }
        }
        
        if (Time.time - episodeStartTime > 120f) // 2-minute episodes
        {
            EndEpisode();
        }

        if (target != null)
        {
            Vector3 toTarget = target.position - transform.position;
            float distanceToTarget = toTarget.magnitude;
            
            // Only give tactical rewards when in actual combat range
            if (distanceToTarget <= 50f) // Within cannon range
            {
                float targetSide = Mathf.Abs(Vector3.Dot(transform.right, toTarget.normalized));
                float targetFront = Mathf.Abs(Vector3.Dot(transform.forward, toTarget.normalized));
                
                // Broadside positioning (only when in range)
                if (targetSide > 0.7f && targetFront < 0.5f)
                {
                    AddReward(0.02f);
                    
                    // Optimal close combat range
                    if (distanceToTarget < 30f && distanceToTarget > 15f) // Sweet spot
                    {
                        AddReward(0.03f);
                    }
                }
            }     
        }

        if (target != null)
        {
            Vector3 toTarget = (target.position - transform.position).normalized;
            
            // Much stricter "crossing the T" requirements
            Vector3 targetForward = target.transform.forward;
            float targetExposure = Mathf.Abs(Vector3.Dot(targetForward, toTarget));
            float ourBroadside = Mathf.Abs(Vector3.Dot(transform.right, toTarget));
            
            // Very strict requirements and much smaller reward
            if (targetExposure > 0.9f && ourBroadside > 0.9f && Vector3.Distance(transform.position, target.position) < 40f)
            {
                AddReward(0.05f); // Much smaller than 0.5f
            }
            
            // Remove or greatly reduce the broadside angle improvement reward
            // This was giving too much reward for just turning
        }

        if (target != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            
            // Strong reward for being in effective combat range
            float maxCombatRange = 50f; // Adjust based on your actual cannon range
            if (distanceToTarget <= maxCombatRange)
            {
                float rangeReward = (maxCombatRange - distanceToTarget) / maxCombatRange * 0.1f;
                AddReward(rangeReward); // Stronger reward for closer combat
            }
            
            // Penalty for being too far apart (out of combat range)
            if (distanceToTarget > maxCombatRange * 1.5f) // 75+ units apart
            {
                AddReward(-0.01f); // Encourage closing distance
            }
            
            // Extra reward for approaching when far apart
            if (distanceToTarget > maxCombatRange)
            {
                Vector3 toTarget = (target.position - transform.position).normalized;
                float approachDot = Vector3.Dot(transform.forward, toTarget);
                if (approachDot > 0.7f) // Moving directly towards target
                {
                    AddReward(0.02f); // Reward for closing distance
                }
            }
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var ca = actionsOut.ContinuousActions;
        ca[0] = Input.GetKey(KeyCode.W) ? 1f : 0f; // Speed input (W key)
        ca[1] = Input.GetAxis("Horizontal"); // Turn input (A/D keys)
        ca[2] = Input.GetKey(KeyCode.Q) ? 1f : Input.GetKey(KeyCode.E) ? -1f : 0f; // Left cannon aim
        ca[3] = Input.GetKey(KeyCode.Q) ? 1f : Input.GetKey(KeyCode.E) ? -1f : 0f; // Right cannon aim

        var da = actionsOut.DiscreteActions;
        da[0] = Input.GetKey(KeyCode.LeftShift) ? 1 : 0; // Fire left cannon
        da[1] = Input.GetKey(KeyCode.Space) ? 1 : 0; // Fire right cannon
    }

    private void MoveForward(float speedInput)
    {
        // Apply forward force based on input
        Vector3 forwardForceVector = transform.forward * (speedInput * forwardForce);
        rb.AddForce(forwardForceVector);

        // Apply drag to prevent infinite acceleration
        Vector3 drag = -rb.linearVelocity * dragCoefficient;
        rb.AddForce(drag);

        // Clamp to maximum speed (but allow natural deceleration)
        float currentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        if (currentSpeed > maxForwardSpeed)
        {
            Vector3 excessVelocity = transform.forward * (currentSpeed - maxForwardSpeed);
            rb.linearVelocity -= excessVelocity;
        }

        // Prevent backward movement
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
        rb.angularVelocity = new Vector3(0f, currentTurnSpeed * Mathf.Deg2Rad, 0f); // Only rotate around Y
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
            AddReward(0.05f); // Small reward for firing
            StartCoroutine(FireCannon(leftCannonMuzzle, "left"));
        }
    }

    private void FireRightCannon()
    {
        if (rightCannonReady)
        {
            AddReward(0.05f); // Small reward for firing
            StartCoroutine(FireCannon(rightCannonMuzzle, "right"));
        }
    }

    private System.Collections.IEnumerator FireCannon(Transform muzzle, string cannon)
    {
        if (cannon == "left")
        {
            leftCannonReady = false;
        }
        else if (cannon == "right")
        {
            rightCannonReady = false;
        }
        
        GameObject cannonball = Instantiate(cannonballPrefab, muzzle.position, muzzle.rotation);
        var cannonballScript = cannonball.GetComponent<CannonballTraining>();
        if (cannonballScript != null)
        {
            cannonballScript.shooter = this;
            cannonballScript.target = target;
        }

        if (cannonball.TryGetComponent<Rigidbody>(out var rbCannon))
        {
            rbCannon.linearVelocity = rb.linearVelocity; // Inherit ship's velocity
            // Fire in the direction the muzzle is pointing
            rbCannon.AddForce(muzzle.forward * cannonFireForce, ForceMode.Impulse);
        }
        
        yield return new WaitForSeconds(reloadTime);

        if (cannon == "left")
            leftCannonReady = true;
        else if (cannon == "right")
            rightCannonReady = true;
    }

    // Called when this agent hits another agent
    public void OnHitOpponent()
    {
        float combatMultiplier = Academy.Instance.EnvironmentParameters.GetWithDefault("combat_focus", 1.0f);
        AddReward(5.0f * combatMultiplier); // Scale hit reward by curriculum
    }

    // Called when this agent is hit by another agent
    public void OnHitByOpponent()
    {
        AddReward(-0.3f); // Increase penalty from -0.2f
        timesHit++;
        if (timesHit >= 5) // Reduce from 5 to 3 for faster episodes
        {
            EndEpisode();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            isCollidingWithObstacle = true;
            
            // Much larger initial crash penalty
            AddReward(-2.0f); // Increased from -0.5f
            
            // Consider ending episode on crash
            if (Vector3.Dot(rb.linearVelocity.normalized, collision.contacts[0].normal) < -0.5f) // Head-on crash
            {
                AddReward(-5.0f); // Massive penalty for head-on crashes
                Debug.Log($"Agent {name} crashed head-on into obstacle!");
            }
        }
        else if (collision.gameObject.CompareTag("Agent"))
        {
            AddReward(-1.0f); // Increased from -0.1f
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            // Stop applying continuous penalty
            isCollidingWithObstacle = false;
        }
    }

    // Called when this agent's cannonball misses everything important
    public void OnMissedShot()
    {
        AddReward(-0.01f); // Small penalty for missing, encourages firing
    }

    private bool CalculateBallisticTrajectory(Transform muzzle, out Vector3 impactPos, out float timeToImpact)
    {
        impactPos = Vector3.zero;
        timeToImpact = 0f;
        
        // Use EXACT same logic as FireCannon
        Vector3 muzzleDirection = muzzle.forward;
        
        // Calculate initial velocity accounting for cannonball mass
        // ForceMode.Impulse applies force/mass, so we need to get the cannonball's mass
        float cannonballMass = 1f; // Default mass, or get from cannonballPrefab if possible
        if (cannonballPrefab != null && cannonballPrefab.TryGetComponent<Rigidbody>(out var prefabRb))
        {
            cannonballMass = prefabRb.mass;
        }
        
        Vector3 cannonForceVelocity = muzzleDirection * (cannonFireForce / cannonballMass);
        Vector3 initialVelocity = rb.linearVelocity + cannonForceVelocity;
        Vector3 startPos = muzzle.position;
        
        // Calculate time for cannonball to reach y = 0 (ground level)
        float gravity = Mathf.Abs(Physics.gravity.y);
        float y0 = startPos.y; // Height above ground
        float v0y = initialVelocity.y; // Initial vertical velocity
        
        // Solve quadratic equation: y = y0 + v0y*t - 0.5*g*t^2
        // When y = 0: 0 = y0 + v0y*t - 0.5*g*t^2
        // Rearranged: 0.5*g*t^2 - v0y*t - y0 = 0
        float a = 0.5f * gravity;
        float b = -v0y;
        float c = -y0;
        
        float discriminant = b * b - 4 * a * c;
        if (discriminant < 0) return false; // No solution - cannonball won't reach ground
        
        float t1 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
        float t2 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);
        
        // Take the positive time that makes sense (cannonball going down)
        timeToImpact = (t1 > 0 && t2 > 0) ? Mathf.Min(t1, t2) : Mathf.Max(t1, t2);
        if (timeToImpact <= 0) return false;
        
        // Calculate where cannonball will land on the ground (y = 0)
        Vector3 horizontalVelocity = new Vector3(initialVelocity.x, 0, initialVelocity.z);
        Vector3 horizontalDisplacement = horizontalVelocity * timeToImpact;
        impactPos = new Vector3(startPos.x + horizontalDisplacement.x, 0f, startPos.z + horizontalDisplacement.z);
        
        return true;
    }

    private bool CalculatePredictiveAiming(out Vector3 predictedTargetPos, out Vector3 cannonballImpactPos)
    {
        predictedTargetPos = Vector3.zero;
        cannonballImpactPos = Vector3.zero;
        
        if (target == null) return false;
        
        // Get target's velocity
        var targetRb = target.GetComponent<Rigidbody>();
        Vector3 targetVelocity = targetRb != null ? targetRb.linearVelocity : Vector3.zero;
        
        // Calculate ballistic trajectory for both cannons
        Vector3 leftImpactPos, rightImpactPos;
        float leftTimeToImpact, rightTimeToImpact;
        bool leftValid = CalculateBallisticTrajectory(leftCannonMuzzle, out leftImpactPos, out leftTimeToImpact);
        bool rightValid = CalculateBallisticTrajectory(rightCannonMuzzle, out rightImpactPos, out rightTimeToImpact);
        
        if (!leftValid && !rightValid) return false;
        
        // Choose the cannon that's pointing more towards the target
        Vector3 toTarget = (target.position - transform.position).normalized;
        float leftDot = leftValid ? Vector3.Dot(leftCannonMuzzle.forward, toTarget) : -1f;
        float rightDot = rightValid ? Vector3.Dot(rightCannonMuzzle.forward, toTarget) : -1f;
        
        Vector3 chosenImpactPos;
        float chosenTimeToImpact;
        Transform chosenMuzzle;
        
        // Choose the cannon that's better aligned with the target
        if (leftValid && rightValid)
        {
            if (leftDot >= rightDot)
            {
                chosenImpactPos = leftImpactPos;
                chosenTimeToImpact = leftTimeToImpact;
                chosenMuzzle = leftCannonMuzzle;
            }
            else
            {
                chosenImpactPos = rightImpactPos;
                chosenTimeToImpact = rightTimeToImpact;
                chosenMuzzle = rightCannonMuzzle;
            }
        }
        else if (leftValid)
        {
            chosenImpactPos = leftImpactPos;
            chosenTimeToImpact = leftTimeToImpact;
            chosenMuzzle = leftCannonMuzzle;
        }
        else
        {
            chosenImpactPos = rightImpactPos;
            chosenTimeToImpact = rightTimeToImpact;
            chosenMuzzle = rightCannonMuzzle;
        }
        
        // Predict where target will be when cannonball hits the ground
        Vector3 targetCurrentPos = target.position;
        Vector3 targetFuturePos = targetCurrentPos + targetVelocity * chosenTimeToImpact;
        
        // Project target's future position onto the ground (y = 0) for fair comparison
        predictedTargetPos = new Vector3(targetFuturePos.x, 0f, targetFuturePos.z);
        cannonballImpactPos = chosenImpactPos; // Already at y = 0
        
        // DEBUG VISUALIZATION
        DrawTrajectoryDebug(chosenMuzzle, chosenImpactPos, predictedTargetPos);
        
        return true;
    }

    private void DrawTrajectoryDebug(Transform muzzle, Vector3 cannonballImpactPos, Vector3 predictedTargetPos)
    {
        if (target == null) return;

        // Calculate aiming error (distance between impact and predicted target position)
        float aimingError = Vector3.Distance(predictedTargetPos, cannonballImpactPos);
        bool isGoodAim = aimingError < 10f; // Adjust this threshold as needed
        Color lineColor = isGoodAim ? Color.green : Color.red;

        // Draw cannonball trajectory line (from muzzle to ground impact point)
        Debug.DrawLine(muzzle.position, cannonballImpactPos, lineColor, 0.1f);
        // Draw target movement prediction line (from current position to predicted position)
        Debug.DrawLine(target.position, predictedTargetPos, Color.blue, 0.1f);

        // Draw aiming error line (from predicted target to cannonball impact)
        Debug.DrawLine(predictedTargetPos, cannonballImpactPos, Color.yellow, 0.1f);
    }
}