using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class EnemyShipAgent : Agent
{
    [Header("Ship Movement")]
    public float forwardSpeed = 10f;
    public float maxTurnSpeed = 60f; // degrees per second
    public float turnAcceleration = 120f; // degrees per second squared

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

    protected override void Awake()
    {
        rb = GetComponent<Rigidbody>();
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
    }

    public override void OnEpisodeBegin()
    {
        // Only the first agent triggers the environment reset
        if (EnvironmentManager.Instance != null && this == EnvironmentManager.Instance.agents[0])
        {
            EnvironmentManager.Instance.ResetEnvironment();
        }

        // Reset ship and cannon states
        transform.localPosition = initialPosition;
        transform.localRotation = initialRotation;
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

        // Find all active agents with the same immediate parent (environment instance)
        var environment = transform.parent;
        var allAgents = environment.GetComponentsInChildren<EnemyShipAgent>(includeInactive: false);

        // Filter out this agent
        var otherAgents = System.Array.FindAll(allAgents, agent => agent != this && agent.gameObject.activeSelf);

        // Assign a random other agent as the target
        EnemyShipAgent opponent = null;
        if (otherAgents.Length > 0)
        {
            opponent = otherAgents[Random.Range(0, otherAgents.Length)];
        }

        target = opponent != null ? opponent.transform : null;

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

        // Cannon angles (normalized)
        sensor.AddObservation(Mathf.InverseLerp(cannonAimMin, cannonAimMax, leftCannonAngle));
        sensor.AddObservation(Mathf.InverseLerp(cannonAimMin, cannonAimMax, rightCannonAngle));

        // Cannon readiness
        sensor.AddObservation(leftCannonReady ? 1f : 0f);
        sensor.AddObservation(rightCannonReady ? 1f : 0f);

        // Target relative position
        if (target != null)
            sensor.AddObservation(target.position - transform.position);
        else
            sensor.AddObservation(Vector3.zero);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float turnInput = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float leftAimInput = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        float rightAimInput = Mathf.Clamp(actions.ContinuousActions[2], -1f, 1f);
        bool fireLeft = actions.DiscreteActions[0] > 0;
        bool fireRight = actions.DiscreteActions[1] > 0;

        MoveForward();
        TurnShip(turnInput);
        AimCannons(leftAimInput, rightAimInput);
        if (fireLeft) FireLeftCannon();
        if (fireRight) FireRightCannon();

        // Increased step penalty to encourage faster action
        AddReward(-0.002f);

        // Reward shaping: distance to target
        if (target != null)
        {
            float prevDist = Vector3.Distance(previousTargetPos, previousAgentPos);
            float currDist = Vector3.Distance(target.position, transform.position);
            float distDelta = prevDist - currDist;
            AddReward(distDelta * 0.05f); // Larger reward for reducing distance

            previousTargetPos = target.position;
            previousAgentPos = transform.position;

            // Reward for aiming cannons toward the target (average of both cannons)
            Vector3 toTarget = (target.position - transform.position).normalized;
            float leftAimDot = Vector3.Dot(leftCannon.forward, toTarget);
            float rightAimDot = Vector3.Dot(rightCannon.forward, toTarget);
            float aimReward = (leftAimDot + rightAimDot) * 0.01f; // Larger reward for aiming
            AddReward(aimReward);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var ca = actionsOut.ContinuousActions;
        ca[0] = Input.GetAxis("Horizontal");
        ca[1] = Input.GetKey(KeyCode.W) ? 1f : Input.GetKey(KeyCode.S) ? -1f : 0f;
        ca[2] = Input.GetKey(KeyCode.UpArrow) ? 1f : Input.GetKey(KeyCode.DownArrow) ? -1f : 0f;

        var da = actionsOut.DiscreteActions;
        da[0] = Input.GetKey(KeyCode.Q) ? 1 : 0;
        da[1] = Input.GetKey(KeyCode.E) ? 1 : 0;
    }

    private void MoveForward()
    {
        Vector3 forwardVelocity = transform.forward * forwardSpeed;
        rb.linearVelocity = new Vector3(forwardVelocity.x, 0f, forwardVelocity.z); // No vertical movement
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
            StartCoroutine(FireCannon(leftCannonMuzzle, Vector3.left, "left"));
        }
    }

    private void FireRightCannon()
    {
        if (rightCannonReady)
            StartCoroutine(FireCannon(rightCannonMuzzle, Vector3.right, "right"));
    }

    private System.Collections.IEnumerator FireCannon(Transform muzzle, Vector3 direction, string cannon)
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
        var cannonballScript = cannonball.GetComponent<Cannonball>();
        if (cannonballScript != null)
            cannonballScript.shooter = this;
            cannonballScript.target = target; 

        if (cannonball.TryGetComponent<Rigidbody>(out var rbCannon))
        {
            rbCannon.linearVelocity = rb.linearVelocity; // Inherit ship's velocity
            rbCannon.AddForce(muzzle.TransformDirection(direction) * cannonFireForce, ForceMode.Impulse);
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
        AddReward(2.0f); // Larger reward for hitting
    }

    // Called when this agent is hit by another agent
    public void OnHitByOpponent()
    {
        AddReward(-0.2f); // Smaller penalty for being hit
        timesHit++;
        if (timesHit >= 5)
        {
            EndEpisode();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            // Stronger penalty for collisions in the forward direction (where rays are densest)
            Vector3 hitDir = collision.contacts[0].normal;
            float rayDensity = Mathf.Max(0f, Vector3.Dot(transform.forward, hitDir)) * 20f; 
            AddReward(-1.0f * rayDensity); // Larger dynamic penalty
            EndEpisode(); // End episode on obstacle hit
        }
        else if (collision.gameObject.CompareTag("Agent"))
        {
            AddReward(-0.1f); // Smaller penalty for bumping another agent
        }
    }

    // Called when this agent's cannonball misses everything important
    public void OnMissedShot()
    {
        AddReward(-0.01f); // Small penalty for missing, encourages firing
    }
}