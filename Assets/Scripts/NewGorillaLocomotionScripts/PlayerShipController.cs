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
    
    [Header("Ship Rigidbody")]
    [SerializeField] private Rigidbody shipRigidbody; // Assign the separate rigidbody in the inspector

    void Start()
    {
        playerRigidbody = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
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

    private void CheckShipContact()
    {
        Vector3[] checkPoints = {
            transform.position,
            transform.position + Vector3.down * 0.5f,
            transform.position + Vector3.up * 0.5f,
            transform.position + transform.forward * 0.3f,
            transform.position - transform.forward * 0.3f,
            transform.position + transform.right * 0.3f,
            transform.position - transform.right * 0.3f
        };

        bool foundShip = false;

        // Check body contact points
        foreach (Vector3 point in checkPoints)
        {
            Collider[] overlapping = Physics.OverlapSphere(point, detectionRadius, shipLayerMask);
            foreach (Collider col in overlapping)
            {
                if (col.CompareTag("Boat"))
                {
                    if (activeShipRigidbody == null && shipRigidbody != null)
                    {
                        activeShipRigidbody = shipRigidbody;
                        Debug.Log("Player boarded ship via body detection");
                    }
                    foundShip = true;
                    break;
                }
            }
            if (foundShip) break;
        }

        // If no ship contact found and we were on one
        if (!foundShip && activeShipRigidbody != null)
        {
            // Add momentum when leaving ship
            playerRigidbody.linearVelocity += lastShipVelocity;
            activeShipRigidbody = null;
            Debug.Log("Player left ship - no body contact");
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
        if (collision.gameObject.CompareTag("Boat") && shipRigidbody != null)
        {
            activeShipRigidbody = shipRigidbody;
            Debug.Log("Player boarded ship via collision");
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Boat") && activeShipRigidbody != null)
        {
            // Let CheckShipContact handle the exit to avoid premature disconnection
            Debug.Log("Player collision exit from ship");
        }
    }
}
