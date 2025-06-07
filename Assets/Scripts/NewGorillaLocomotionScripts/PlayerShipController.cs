using UnityEngine;

public class PlayerShipController : MonoBehaviour
{
    private Rigidbody playerRigidbody;
    private Rigidbody activeShipRigidbody;
    private Vector3 lastShipVelocity;
    private Vector3 playerVelocityBeforeShip; // Store player's original velocity

    [Header("Ship Detection")]
    public LayerMask shipLayerMask = -1;
    public float detectionRadius = 0.2f;
    
    [Header("Hand Detection")]
    public bool useHandDetection = true;
    private int handsOnShip = 0; // Count of hands touching ship
    private Rigidbody handDetectedShip = null; // Ship detected by hands

    void Start()
    {
        playerRigidbody = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // Enhanced detection for handstands
        CheckShipContact();

        if (activeShipRigidbody != null)
        {
            Vector3 pointVelocity = CalculatePointVelocity();
            lastShipVelocity = pointVelocity;

            // CORRECTED: Set player velocity to ship velocity + relative movement
            // Don't use MovePosition as it compounds movement
            Vector3 currentPlayerVelocity = playerRigidbody.linearVelocity;
            Vector3 relativeVelocity = currentPlayerVelocity - lastShipVelocity;
            
            // Apply ship movement while preserving player's relative movement
            playerRigidbody.linearVelocity = pointVelocity + relativeVelocity;
        }
    }

    // ADD THIS METHOD - Called by HandShipDetector
    public void OnHandTouchingShip(Rigidbody shipRigidbody)
    {
        if (shipRigidbody != null)
        {
            // Hand is touching a ship
            handsOnShip++;
            handDetectedShip = shipRigidbody;
            
            Debug.Log($"Hand touching ship. Total hands: {handsOnShip}");
            
            // If we don't have an active ship connection, use the hand-detected ship
            if (activeShipRigidbody == null)
            {
                activeShipRigidbody = shipRigidbody;
                Debug.Log("Set active ship via hand contact");
            }
        }
        else
        {
            // Hand left the ship
            handsOnShip = Mathf.Max(0, handsOnShip - 1);
            Debug.Log($"Hand left ship. Total hands: {handsOnShip}");
            
            // If no hands are touching, clear hand-detected ship
            if (handsOnShip == 0)
            {
                handDetectedShip = null;
            }
        }
    }

    private void CheckShipContact()
    {
        // Multiple detection points for handstand support
        Vector3[] checkPoints = {
            transform.position,
            transform.position + Vector3.down * 0.5f,
            transform.position + Vector3.up * 0.5f, // For handstands
            transform.position + transform.forward * 0.3f,
            transform.position - transform.forward * 0.3f,
            transform.position + transform.right * 0.3f,
            transform.position - transform.right * 0.3f
        };

        bool foundShipViaBody = false;

        // Check body contact points
        foreach (Vector3 point in checkPoints)
        {
            Collider[] overlapping = Physics.OverlapSphere(point, detectionRadius, shipLayerMask);
            foreach (Collider col in overlapping)
            {
                if (col.CompareTag("Boat"))
                {
                    if (activeShipRigidbody == null)
                    {
                        activeShipRigidbody = col.attachedRigidbody;
                        Debug.Log("Player boarded ship via body detection");
                    }
                    foundShipViaBody = true;
                    break;
                }
            }
            if (foundShipViaBody) break;
        }

        // ENHANCED: Also consider ship contact if hands are touching
        bool foundShipViaHands = false;
        if (useHandDetection && handsOnShip > 0 && handDetectedShip != null)
        {
            foundShipViaHands = true;
            
            // If we don't have an active ship but hands are touching, use hand-detected ship
            if (activeShipRigidbody == null)
            {
                activeShipRigidbody = handDetectedShip;
                Debug.Log("Set active ship via hand detection in CheckShipContact");
            }
        }

        // COMBINED: Player is connected if EITHER body OR hands are touching
        bool foundShip = foundShipViaBody || foundShipViaHands;

        // Debug info
        if (foundShipViaHands && !foundShipViaBody)
        {
            Debug.Log("Player connected to ship via hands only");
        }

        // If no ship contact found and we were on one
        if (!foundShip && activeShipRigidbody != null)
        {
            // Only disconnect if hands aren't touching either
            if (!useHandDetection || handsOnShip == 0)
            {
                // Add momentum when leaving ship
                playerRigidbody.linearVelocity += lastShipVelocity;
                activeShipRigidbody = null;
                Debug.Log("Player left ship - no body or hand contact");
            }
        }
    }

    private Vector3 CalculatePointVelocity()
    {
        Vector3 linearVelocity = activeShipRigidbody.linearVelocity;
        Vector3 angularVelocity = activeShipRigidbody.angularVelocity;
        Vector3 relativePos = playerRigidbody.position - activeShipRigidbody.worldCenterOfMass;
        Vector3 rotationalVelocity = Vector3.Cross(angularVelocity, relativePos);
        return linearVelocity + rotationalVelocity;
    }

    // Keep collision detection as backup
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Boat"))
        {
            activeShipRigidbody = collision.rigidbody;
            Debug.Log("Player boarded ship via collision");
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (activeShipRigidbody != null && collision.rigidbody == activeShipRigidbody)
        {
            // Let CheckShipContact handle the exit to avoid premature disconnection
            Debug.Log("Player collision exit from ship");
        }
    }
}
