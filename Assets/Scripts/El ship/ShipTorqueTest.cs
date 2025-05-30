using System;
using Unity.Mathematics;
using UnityEngine;

public class ShipTorqueTest : MonoBehaviour
{
    private float torque;
    //private Rigidbody rb;
    [SerializeField] public SteeringWheelHingeJoint SteeringWheel;

    [SerializeField] private float rotSpeed = 10f; // Speed of rotation

    //[SerializeField] public RopePulleyInteractor rope;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //rb = GetComponent<Rigidbody>();

    }

    // Update is called once per frame
    void Update()
    {
        torque = SteeringWheel.CurrentAngle;
        if (math.abs(torque) > 5) //speelruimte voor geen perma draaing te krijgen
        {
            //rb.AddRelativeTorque(transform.up * torque * Time.deltaTime, ForceMode.VelocityChange);
            transform.Rotate(Vector3.down, torque/SteeringWheel.maxRotation * rotSpeed * Time.deltaTime, Space.Self);
        }

        

    }
}
