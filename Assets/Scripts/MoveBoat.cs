using UnityEngine;

public class MoveBoat : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private SharedPulleyValue sharedPulleyValue; // Assign in inspector
    [SerializeField] private float maxForce = 10f; // Maximum force to apply
    [SerializeField] private float maxSpeed = 5f; // Maximum speed in units per second
    [SerializeField] private Vector3 moveDirection = Vector3.forward; // Direction to move
    
    [Header("Steering Settings")]
    [SerializeField] private float maxTurnSpeed = 60f; // Maximum turn speed in degrees per second
    [SerializeField] private float steeringDeadzone = 0.1f; // Minimum steering input to start turning
    [SerializeField] private SteeringWheelHingeJoint steeringWheel; // Assign the steering wheel
    [SerializeField] private float steeringDamping = 5f; // How quickly steering changes are applied
    
    private Rigidbody rb;
    private float currentSteeringValue = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("MoveBoat requires a Rigidbody component!");
        }

        // Subscribe to steering wheel events
        if (steeringWheel != null)
        {
            steeringWheel.OnWheelRotated.AddListener(OnSteeringInput);
        }
    }

    void Update()
    {
        if (rb == null) return;

        HandleMovement();
        HandleSteering();
    }

    private void HandleMovement()
    {
        if (sharedPulleyValue == null) return;

        // Use pulley value as percentage (0-100)
        float pulleyPercentage = sharedPulleyValue.Value / 100f;
        
        // Calculate force and speed based on pulley value
        float currentMaxSpeed = maxSpeed * pulleyPercentage;
        float force = maxForce * pulleyPercentage;
        
        // Apply force in the ship's forward direction (accounting for rotation)
        Vector3 forwardDirection = transform.TransformDirection(moveDirection.normalized);
        
        // Only apply force if we haven't reached max speed
        if (rb.linearVelocity.magnitude < currentMaxSpeed)
        {
            rb.AddForce(forwardDirection * force * Time.deltaTime, ForceMode.Acceleration);
        }
        
        // Clamp velocity to current max speed
        if (rb.linearVelocity.magnitude > currentMaxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * currentMaxSpeed;
        }
    }

    private void HandleSteering()
    {
        // Apply deadzone to prevent micro-movements
        float steeringInput = Mathf.Abs(currentSteeringValue) > steeringDeadzone ? currentSteeringValue : 0f;
        
        // Calculate target angular velocity based on steering input (INVERTED)
        float targetAngularVelocity = -steeringInput * maxTurnSpeed * Mathf.Deg2Rad;
        
        // Get current angular velocity and smoothly change it
        Vector3 currentAngularVel = rb.angularVelocity;
        Vector3 targetAngularVel = new Vector3(0, targetAngularVelocity, 0);
        
        // Smoothly interpolate to target angular velocity
        Vector3 newAngularVel = Vector3.Lerp(currentAngularVel, targetAngularVel, steeringDamping * Time.deltaTime);
        rb.angularVelocity = newAngularVel;
    }

    private void OnSteeringInput(float steeringValue)
    {
        currentSteeringValue = steeringValue;
        Debug.Log($"Steering input: {steeringValue:F2}");
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (steeringWheel != null)
        {
            steeringWheel.OnWheelRotated.RemoveListener(OnSteeringInput);
        }
    }
}
