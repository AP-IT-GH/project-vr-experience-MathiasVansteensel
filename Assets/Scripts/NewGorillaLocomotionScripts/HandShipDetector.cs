using UnityEngine;


public class HandShipDetector : MonoBehaviour
{
    private PlayerShipController playerShipController;
    
    void Start()
    {
        // Find the PlayerShipController
        playerShipController = FindFirstObjectByType<PlayerShipController>();
        
        // Add safe Rigidbody for trigger detection
        Rigidbody handRb = GetComponent<Rigidbody>();
        if (handRb == null)
        {
            handRb = gameObject.AddComponent<Rigidbody>();
            
            // CRITICAL: Safe settings that won't interfere with XR
            handRb.isKinematic = true;        // No physics simulation
            handRb.useGravity = false;        // No gravity
            handRb.mass = 0.1f;              // Very light (in case kinematic gets disabled)
            handRb.linearDamping  = 0f;                // No air resistance  
            handRb.angularDamping  = 0f;         // No rotational resistance
            handRb.freezeRotation = true;    // Prevent unwanted rotation
            
            Debug.Log($"Added safe Rigidbody to {gameObject.name} for trigger detection");
        }
        
        // Make sure the existing collider is set up properly
        SphereCollider existingCollider = GetComponent<SphereCollider>();
        if (existingCollider != null)
        {
            // The collider should already be a trigger for XR interaction
            // but let's make sure it can detect ship layers
            Debug.Log($"Using existing Direct Interactor collider: radius {existingCollider.radius}");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Boat") && playerShipController != null)
        {
            Rigidbody shipRigidbody = other.GetComponentInParent<Rigidbody>();

            if (shipRigidbody == null)
            {
                Debug.LogWarning("Detected a Boat without a Rigidbody. Please ensure the ship has a Rigidbody component.");
                return;
            }
            else
            {
                Debug.Log($"Hand touching ship: {other.name}");
                playerShipController.OnHandTouchingShip(shipRigidbody);
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Boat") && playerShipController != null)
        {
            Debug.Log($"Hand stopped touching ship: {other.name}");
            playerShipController.OnHandTouchingShip(null);
        }
    }
}