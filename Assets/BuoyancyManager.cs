using MathiasCode;
using UnityEngine;

public class BuoyancyManager : MonoBehaviour
{
    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            rb.AddForce(-rb.transform.right * Time.fixedDeltaTime, ForceMode.VelocityChange);
        }
    }
}
