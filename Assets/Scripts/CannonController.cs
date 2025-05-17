using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class CannonController : XRBaseInteractable
{
    [SerializeField] private Transform barrelTransform;
    [SerializeField] private Transform baseTransform;
    [SerializeField] private GameObject cannonBallPrefab;
    [SerializeField] private Transform cannonBallSpawnPoint;
    [SerializeField] private InputActionReference leftTriggerActionReference;
    [SerializeField] private InputActionReference rightTriggerActionReference;
    [SerializeField] private float reloadTime = 2.0f; // Time in seconds to reload the cannon
    [SerializeField] private float cannonFireForce = 1000.0f; // Force applied to the cannonball

    private float currentBarrelAngle = 0.0f;
    private float currentBaseAngle = 0.0f;
    private bool isReloading = false; // Tracks if the cannon is reloading

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        currentBarrelAngle = FindBarrelAngle();
        currentBaseAngle = FindBaseAngle();
        if (args.interactorObject.handedness == InteractorHandedness.Left)
        {
            leftTriggerActionReference.action.performed += ShootCannonball;
        }
        else if (args.interactorObject.handedness == InteractorHandedness.Right)
        {
            rightTriggerActionReference.action.performed += ShootCannonball;
        }
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        currentBarrelAngle = FindBarrelAngle();
        currentBaseAngle = FindBaseAngle();
        leftTriggerActionReference.action.performed -= ShootCannonball;
        rightTriggerActionReference.action.performed -= ShootCannonball;
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);

        if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
        {
            if (isSelected)
            {
                RotateBarrel();
                RotateBase();
            }
        }
    }

    private void RotateBarrel()
    {
        // Convert that direction to an angle, then rotation
        float totalAngle = FindBarrelAngle();

        // Apply the corrected difference in angle to the barrel in local space
        float angleDifference = totalAngle - currentBarrelAngle; // Invert the direction
        barrelTransform.localRotation *= Quaternion.Euler(0, 0, angleDifference);

        // Store angle for next process
        currentBarrelAngle = totalAngle;
    }

    private void RotateBase()
    {
        // Convert that direction to an angle, then rotation
        float totalAngle = FindBaseAngle();

        // Apply the inverted difference in angle to the base
        float angleDifference = currentBaseAngle - totalAngle; // Correctly invert the direction
        baseTransform.Rotate(Vector3.up, angleDifference, Space.World);

        // Store angle for next process
        currentBaseAngle = totalAngle;
    }

    private float FindBarrelAngle()
    {
        float totalAngle = 0;

        // Combine directions of current interactors
        foreach (IXRSelectInteractor interactor in interactorsSelecting)
        {
            Vector2 direction = FindLocalPoint(interactor.transform.position);
            totalAngle += ConvertToAngle(direction) * FindRotationSensitivity();
        }

        return totalAngle;
    }

    private float FindBaseAngle()
    {
        float totalAngle = 0;

        // Combine directions of current interactors
        foreach (IXRSelectInteractor interactor in interactorsSelecting)
        {
            Vector2 direction = FindLocalPointOnXZ(interactor.transform.position);
            totalAngle += ConvertToAngleOnXZ(direction) * FindRotationSensitivity();
        }

        return totalAngle;
    }

    private Vector2 FindLocalPoint(Vector3 position)
    {
        // Convert the hand positions to local, so we can find the angle easier
        return transform.InverseTransformPoint(position).normalized;
    }

    private Vector2 FindLocalPointOnXZ(Vector3 position)
    {
        // Convert the hand positions to local, so we can find the angle easier
        Vector3 localPosition = transform.InverseTransformPoint(position);
        return new Vector2(localPosition.x, localPosition.z).normalized;
    }

    private float ConvertToAngle(Vector2 direction)
    {
        // Use a consistent up direction to find the angle
        return Vector2.SignedAngle(Vector2.up, direction);
    }

    private float ConvertToAngleOnXZ(Vector2 direction)
    {
        // Use a consistent forward direction to find the angle
        return Vector2.SignedAngle(Vector2.up, direction);
    }

    private float FindRotationSensitivity()
    {
        // Use a smaller rotation sensitivity with two hands
        return 1.0f / interactorsSelecting.Count;
    }
    private void ShootCannonball(InputAction.CallbackContext context)
    {
        if (isReloading) return; // Prevent firing if the cannon is reloading

        if (cannonBallPrefab != null && cannonBallSpawnPoint != null)
        {
            // Instantiate the cannonball at the spawn point
            GameObject cannonBall = Instantiate(cannonBallPrefab, cannonBallSpawnPoint.position, cannonBallSpawnPoint.rotation);

            // Add force to the cannonball to shoot it forward
            if (cannonBall.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.AddForce(cannonBallSpawnPoint.forward * cannonFireForce); // Adjust force as needed
            }

            // Start the reload process
            StartCoroutine(ReloadCannon());
        }
    }

    private IEnumerator ReloadCannon()
    {
        isReloading = true; // Set reloading flag
        yield return new WaitForSeconds(reloadTime); // Wait for the reload time
        isReloading = false; // Reset reloading flag
    }
}