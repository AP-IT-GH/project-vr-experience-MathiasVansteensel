
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class SteeringWheel : UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable
{
    [SerializeField] private Transform wheelTransform;
    [SerializeField] public float maxRotation = 180f;

    public UnityEvent<float> OnWheelRotated;

    private float currentAngle = 0.0f;
    public float totalRotation = 0.0f; // Tracks cumulative rotation

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        currentAngle = FindWheelAngle();
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        currentAngle = FindWheelAngle();
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);

        if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
        {
            if (isSelected)
                RotateWheel();
        }
    }

    private void RotateWheel()
    {
        float totalAngle = FindWheelAngle();
        float angleDifference = currentAngle - totalAngle;

        // Check if we can rotate further in this direction
        if (CanRotate(angleDifference))
        {
            wheelTransform.Rotate(transform.right, -angleDifference, Space.World);
            totalRotation += angleDifference;
            OnWheelRotated?.Invoke(angleDifference);
        }

        currentAngle = totalAngle;
    }

    private bool CanRotate(float angleDifference)
    {
        // Check if trying to rotate beyond limits
        if (angleDifference > 0) // Rotating clockwise (right)
        {
            return totalRotation + angleDifference <= maxRotation;
        }
        else // Rotating counter-clockwise (left)
        {
            return totalRotation + angleDifference >= -maxRotation;
        }
    }

    private float FindWheelAngle()
    {
        float totalAngle = 0;

        // Combine directions of current interactors
        foreach (UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor interactor in interactorsSelecting)
        {
            Vector2 direction = FindLocalPoint(interactor.transform.position);
            totalAngle += ConvertToAngle(direction) * FindRotationSensitivity();
        }

        return totalAngle;
    }

    private Vector2 FindLocalPoint(Vector3 position)
    {
        // Convert the hand positions to local, so we can find the angle easier
        return transform.InverseTransformPoint(position).normalized;
    }

    private float ConvertToAngle(Vector2 direction)
    {
        // Use a consistent up direction to find the angle
        return Vector2.SignedAngle(Vector2.up, direction);
    }

    private float FindRotationSensitivity()
    {
        // Use a smaller rotation sensitivity with two hands
        return 1.0f / interactorsSelecting.Count;
    }

    // Reset the wheel rotation (optional)
    public void ResetWheel()
    {
        totalRotation = 0f;
        wheelTransform.localRotation = Quaternion.identity;
    }
}

//using UnityEngine;
//using UnityEngine.Events;
//using UnityEngine.XR.Interaction.Toolkit;
//using UnityEngine.XR.Interaction.Toolkit.Interactables;

//public class SteeringWheel : XRBaseInteractable
//{
//    [Header("Steering Wheel Settings")]
//    [SerializeField] private Transform wheelTransform;
//    [SerializeField] public float maxRotation = 180f;
//    [SerializeField] private float rotationSpeed = 2f; // Sensitivity multiplier

//    [Header("Events")]
//    public UnityEvent<float> OnWheelRotated; // Sends angle change in degrees

//    public float totalRotation = 0f;
//    private Vector3[] handLocalPositions;
//    private float[] handStartAngles;
//    private bool wasTwoHanded = false;

//    protected override void OnSelectEntered(SelectEnterEventArgs args)
//    {
//        base.OnSelectEntered(args);
//        UpdateHandData();
//    }

//    protected override void OnSelectExited(SelectExitEventArgs args)
//    {
//        base.OnSelectExited(args);
//        UpdateHandData();
//    }

//    private void UpdateHandData()
//    {
//        int handCount = interactorsSelecting.Count;
//        handLocalPositions = new Vector3[handCount];
//        handStartAngles = new float[handCount];

//        for (int i = 0; i < handCount; i++)
//        {
//            handLocalPositions[i] = wheelTransform.InverseTransformPoint(
//                interactorsSelecting[i].transform.position);
//            handStartAngles[i] = GetHandAngle(handLocalPositions[i]);
//        }

//        // Store if this was a two-handed grab for smooth transition
//        wasTwoHanded = handCount == 2;
//    }

//    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
//    {
//        base.ProcessInteractable(updatePhase);

//        if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic && isSelected)
//        {
//            RotateWheel();
//        }
//    }

//    private void RotateWheel()
//    {
//        int handCount = interactorsSelecting.Count;
//        float totalAngle = 0f;

//        for (int i = 0; i < handCount; i++)
//        {
//            // Get current hand position in local space
//            Vector3 currentHandLocalPos = wheelTransform.InverseTransformPoint(
//                interactorsSelecting[i].transform.position);

//            // Get current angle
//            float currentHandAngle = GetHandAngle(currentHandLocalPos);

//            // Calculate difference from start angle
//            float angleDifference = Mathf.DeltaAngle(handStartAngles[i], currentHandAngle);

//            // Add to total with sensitivity adjustment
//            totalAngle += angleDifference * rotationSpeed;

//            // Update stored position for next frame
//            handLocalPositions[i] = currentHandLocalPos;
//        }

//        // Average if two-handed
//        if (handCount == 2)
//        {
//            totalAngle *= 0.5f;
//        }
//        // If switching between one and two hands, reset the start angles to prevent jump
//        else if (wasTwoHanded)
//        {
//            wasTwoHanded = false;
//            UpdateHandData();
//            return;
//        }

//        // Clamp the rotation to max limits
//        float newRotation = Mathf.Clamp(totalRotation + totalAngle, -maxRotation, maxRotation);
//        float actualRotation = newRotation - totalRotation;

//        if (Mathf.Abs(actualRotation) > 0.01f)
//        {
//            // Apply the rotation locally
//            wheelTransform.localRotation = Quaternion.Euler(actualRotation, 0, 0) * wheelTransform.localRotation;
//            totalRotation = newRotation;
//            OnWheelRotated?.Invoke(actualRotation);
//        }

//        // Update start angles for next frame
//        for (int i = 0; i < handCount; i++)
//        {
//            handStartAngles[i] = GetHandAngle(handLocalPositions[i]);
//        }
//    }

//    private float GetHandAngle(Vector3 localPosition)
//    {
//        // Convert 3D local position to 2D plane (ignoring forward/back axis)
//        Vector2 direction = new Vector2(localPosition.y, localPosition.z).normalized;

//        // Return signed angle from up vector
//        return Vector2.SignedAngle(Vector2.up, direction);
//    }

//    public void ResetWheel()
//    {
//        totalRotation = 0f;
//        wheelTransform.localRotation = Quaternion.identity;
//    }

//    public float GetNormalizedRotation()
//    {
//        return Mathf.Clamp(totalRotation / maxRotation, -1f, 1f);
//    }
//}