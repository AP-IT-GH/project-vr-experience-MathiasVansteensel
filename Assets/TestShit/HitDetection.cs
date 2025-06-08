using UnityEngine;

public class HitDetection : MonoBehaviour
{
    void Start()
    {
        
    }

    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        //check if not hit player
        //get character script
        //hit
        //TODO: death :)
        bool hasValidContact = false;
        for (int i = 0; i < collision.contactCount; i++) 
        {
            ContactPoint p = collision.GetContact(i);
            //if (p.thisCollider == )
        }
        Debug.Log(collision.gameObject);
    }
}
