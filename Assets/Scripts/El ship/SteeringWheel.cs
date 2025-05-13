// using UnityEngine;
// using UnityEngine.Events;
// using UnityEngine.XR.Interaction.Toolkit;

// public class SteeringWheel : UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable
// {
//     [SerializeField] private Transform wheelTransform;

//     public UnityEvent<float> OnWheelRotated;

//     private float currentAngle = 0.0f;

//     protected override void OnSelectEntered(SelectEnterEventArgs args)
//     {
//         base.OnSelectEntered(args);
//         currentAngle = FindWheelAngle();
//     }

//     protected override void OnSelectExited(SelectExitEventArgs args)
//     {
//         base.OnSelectExited(args);
//         currentAngle = FindWheelAngle();
//     }

//     public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
//     {
//         base.ProcessInteractable(updatePhase);

//         if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
//         {
//             if (isSelected)
//                 RotateWheel();
//         }
//     }

//     private void RotateWheel()
//     {
//         // Convert that direction to an angle, then rotation
//         float totalAngle = FindWheelAngle();

//         // Apply difference in angle to wheel
//         float angleDifference = currentAngle - totalAngle;
//         wheelTransform.Rotate(transform.forward, -angleDifference, Space.World);
            
//         // Store angle for next process
//         currentAngle = totalAngle;
//         OnWheelRotated?.Invoke(angleDifference);
//     }

//     private float FindWheelAngle()
//     {
//         float totalAngle = 0;

//         // Combine directions of current interactors
//         foreach (UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor interactor in interactorsSelecting)
//         {
//             Vector2 direction = FindLocalPoint(interactor.transform.position);
//             totalAngle += ConvertToAngle(direction) * FindRotationSensitivity();
//         }

//         return totalAngle;
//     }

//     private Vector2 FindLocalPoint(Vector3 position)
//     {
//         // Convert the hand positions to local, so we can find the angle easier
//         return transform.InverseTransformPoint(position).normalized;
//     }

//     private float ConvertToAngle(Vector2 direction)
//     {
//         // Use a consistent up direction to find the angle
//         return Vector2.SignedAngle(Vector2.up, direction);
//     }

//     private float FindRotationSensitivity()
//     {
//         // Use a smaller rotation sensitivity with two hands
//         return 1.0f / interactorsSelecting.Count;
//     }
// }

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
            wheelTransform.Rotate(transform.forward, -angleDifference, Space.World);
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