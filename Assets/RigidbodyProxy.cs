using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyProxy : MonoBehaviour
{
    public Transform target;

    private Rigidbody rb;
    void Start()
    {
        if (target == null) throw new NullReferenceException("Rigidbody proxy target not set up");

        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        target.position = rb.position;
        target.rotation = rb.rotation;
    }
}
