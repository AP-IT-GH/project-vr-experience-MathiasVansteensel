// using UnityEngine;
// using UnityEngine.Events;
// using UnityEngine.XR.Interaction.Toolkit;
// using UnityEngine.XR.Interaction.Toolkit.Interactables;
// using UnityEngine.XR.Interaction.Toolkit.Interactors;

// public class SteeringWheelHingeJoint : XRBaseInteractable
// {
//     [Header("Steering Wheel Settings")]
//     //[SerializeField] private HingeJoint wheelHingeJoint; // Assign the Hinge Joint here
//     [SerializeField] public float maxRotation = 180f; // Total rotation from center to one side
//     [SerializeField] private float rotationSensitivity = 500f; // Multiplier for motor speed

//     [Header("Events")]
//     public UnityEvent<float> OnWheelRotated; // Sends normalized rotation (-1 to 1)

//     private float currentHandAngle = 0.0f;
//     private float totalRotation = 0.0f; // Tracks cumulative rotation from the center
//     private const float motorForce = 1000f; // Constant force for the Hinge Joint motor
//     private float handStartOffset = 0.0f;
//     private bool isInteracting = false; // Track if the player is interacting with the wheel
//     private Transform interactorTransform; // Reference to the interactor's transform

//     private void FixedUpdate()
//     {
//         if (!isInteracting) return;

//         float axisOffset = GetAxisOffset(); // Get the current axis offset
//         float axisDelta = handStartOffset - axisOffset;
//         Debug.Log($"Starting Pos: {handStartOffset}, Axis Offset: {axisOffset}, Delta: {axisDelta}");

//         transform.Rotate(transform.right, axisDelta * 10);
//     }

//     private float GetAxisOffset()
//     {
//         Vector3 playerPos = interactorTransform.position;
//         Vector3 localInteractorPos = transform.InverseTransformPoint(playerPos);
//         return localInteractorPos.z;
//     }

//     protected override void OnSelectEntered(SelectEnterEventArgs args)
//     {
//         base.OnSelectEntered(args);
//         interactorTransform = args.interactorObject.transform;
//         handStartOffset = GetAxisOffset(); // Store the initial position of the player's hand
//         isInteracting = true; // Set interaction state
//     }

//     protected override void OnSelectExited(SelectExitEventArgs args)
//     {
//         base.OnSelectExited(args);
//         handStartOffset = 0f; // Reset player start position
//         isInteracting = false; // Reset interaction state
//     }
// }
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SteeringWheelHingeJoint : XRBaseInteractable
{
    [Header("Steering Wheel Settings")]
    [SerializeField] public float maxRotation = 180f;
    [SerializeField] private float rotationSensitivity = 1f; // Adjust to control speed

    [Header("Events")]
    public UnityEvent<float> OnWheelRotated; // Normalized (-1 to 1)

    private float totalRotation = 0f;
    private float startAngle = 0f;
    private bool isInteracting = false;
    private Transform interactorTransform;

    public float CurrentAngle { get; private set; }

    private void FixedUpdate()
    {
        if (!isInteracting || interactorTransform == null) return;

        float currentAngle = GetInteractorAngle();
        float angleDelta = Mathf.DeltaAngle(startAngle, currentAngle) * rotationSensitivity;

        totalRotation += angleDelta;
        totalRotation = Mathf.Clamp(totalRotation, -maxRotation, maxRotation);

        // Apply rotation
        transform.localRotation = Quaternion.Euler(totalRotation, 0, 0); // assuming local X axis is rotation axis

        CurrentAngle = totalRotation;
        OnWheelRotated?.Invoke(CurrentAngle / maxRotation);

        // Update for next frame
        startAngle = currentAngle;
    }

    private float GetInteractorAngle()
    {
        Vector3 dir = interactorTransform.position - transform.position;
        Vector3 projected = Vector3.ProjectOnPlane(dir, transform.right); // Project onto wheel's rotation plane
        return Mathf.Atan2(projected.z, projected.y) * Mathf.Rad2Deg;
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        interactorTransform = args.interactorObject.transform;
        startAngle = GetInteractorAngle();
        isInteracting = true;
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        isInteracting = false;
        interactorTransform = null;
    }
}
