using System;
using Unity.Mathematics;
using UnityEngine;

public class ShipTorqueTest : MonoBehaviour
{
    private float torque;
    private Rigidbody rb;
    [SerializeField] public SteeringWheel SteeringWheel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();

    }

    // Update is called once per frame
    void Update()
    {
        torque = SteeringWheel.totalRotation;
        if(math.abs(torque) > 5) //speelruimte voor geen perma draaing te krijgen
        {
            rb.AddRelativeTorque(transform.up * torque/SteeringWheel.maxRotation);
        }

    }
}
