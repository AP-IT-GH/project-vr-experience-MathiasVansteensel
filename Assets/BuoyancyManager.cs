using MathiasCode;
using System;
using TreeEditor;
using UnityEngine;

public class BuoyancyManager : MonoBehaviour
{
    private Rigidbody rb;
    public Transform target;
    public float waveIntensity = 10f;
    public float waveSpeed = 0.1f;

    public float xRockingSpeed = 1.5f;
    public float xRockingAmplitude = 10f;
    public float zRockingSpeed = 1.5f;
    public float zRockingAmplitude = 10f;

    public PID3DSettings settings;
    PID3D pid;

    Perlin p;
    void Start()
    {
        pid = new(settings);
        p = new();
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float buoyancyVal = waveIntensity * p.Noise(Time.fixedTime * waveSpeed);
        float xRot = xRockingAmplitude * MathF.Sin(Time.fixedTime * xRockingSpeed) + (xRockingAmplitude / 2f) * MathF.Sin(Time.fixedTime * xRockingSpeed / 3f + 10f);
        float zRot = zRockingAmplitude * MathF.Sin(Time.fixedTime * zRockingSpeed) + (zRockingAmplitude / 2f) * MathF.Sin(Time.fixedTime * zRockingSpeed / 3f + 25f);

        xRot *= Time.fixedDeltaTime;
        zRot *= Time.fixedDeltaTime;

        rb.AddRelativeTorque(xRot, 0, zRot);

        Vector3 setpoint = target.position + new Vector3(0, buoyancyVal, 0);
        pid.Setpoint = setpoint;
        pid.ProcessValue = rb.position;

        Vector3 correctionForce = pid.Tick(out _, out _, out _, out _);
        rb.AddForce(correctionForce * Time.fixedDeltaTime);

        //if (Input.GetKey(KeyCode.UpArrow))
        //{

        //    rb.AddForce(-rb.transform.right * Time.fixedDeltaTime, ForceMode.VelocityChange);
        //}
    }

    private void OnDrawGizmos()
    {
        if (pid == null) return;
        Gizmos.DrawWireSphere(pid.ProcessValue, 1);
        Gizmos.DrawWireSphere(pid.Setpoint, 1);
        Gizmos.DrawLine(pid.ProcessValue, pid.Setpoint);
    }
}
