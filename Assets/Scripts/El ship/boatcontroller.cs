using UnityEngine;

public class BoatController : MonoBehaviour
{
    public float rotationSpeed = 100f;
    [SerializeField] private CannonController cannonControllerL;
    [SerializeField] private CannonController cannonControllerR;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log("Q key pressed, fire left cannon");
            cannonControllerL.ShootCannonball();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("E key pressed, fire right cannon");
            cannonControllerR.ShootCannonball();
        }


        float rotation = 0f;

        if (Input.GetKey(KeyCode.Q))
        {
            rotation = -rotationSpeed * Time.deltaTime;
            Debug.Log("Turning left");
        }

        if (Input.GetKey(KeyCode.D))
        {
            rotation = rotationSpeed * Time.deltaTime;
            Debug.Log("Turning right");
        }

        transform.Rotate(0f, rotation, 0f);
    }
}
