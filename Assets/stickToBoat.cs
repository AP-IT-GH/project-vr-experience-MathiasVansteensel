using UnityEngine;

// HOW TO USE:
// ATTACH SCRIPT TO PLAYER XR RIG GORILLA/PRFB
// ATTACH TRIGGER COLLIDER TO BOAT (part of the boat that handles turning/movement))
// MAKE SURE TRIGGER COLLIDER IS SET TO "IS TRIGGER"

public class stickToBoat : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Boat"))
        {
            transform.SetParent(other.transform);
        }
    }

    // When the player exits the boat's trigger, unparent
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Boat"))
        {
            transform.SetParent(null);
        }
    }
}
