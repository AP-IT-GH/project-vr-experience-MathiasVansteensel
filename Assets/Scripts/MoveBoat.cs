using UnityEngine;

public class MoveBoat : MonoBehaviour
{
    [SerializeField] private SharedPulleyValue sharedPulleyValue; // Assign in inspector
    [SerializeField] private float maxSpeed = 5f; // Maximum speed in units per second
    [SerializeField] private Vector3 moveDirection = Vector3.forward; // Direction to move

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (sharedPulleyValue == null) return;

        // Use pulley value as percentage (0-100)
        float speed = maxSpeed * (sharedPulleyValue.Value / 100f);
        transform.position += moveDirection.normalized * speed * Time.deltaTime;
    }
}
