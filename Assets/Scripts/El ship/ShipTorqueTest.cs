using System;
using Unity.Mathematics;
using UnityEngine;

public class ShipTorqueTest : MonoBehaviour
{
    private float torque;
    [SerializeField] public SteeringWheelHingeJoint SteeringWheel;
    [SerializeField] private float rotSpeed = 10f; // Speed of rotation

    [Header("Ship Health")]
    [SerializeField] private int maxHP = 3;
    private int currentHP;

    [Header("Ship Destroyed VFX")]
    [SerializeField] private GameObject destroyedVFXPrefab;

    [SerializeField] private GameObject shipRespawPoint;

    [Header("Player Fade Reference")]
    [SerializeField] private PlayerFallRespawn playerRespawn; // Assign in inspector

    private bool isRespawning = false;

    void Start()
    {
        currentHP = maxHP;
    }

    void Update()
    {
        torque = SteeringWheel.CurrentAngle;
        if (math.abs(torque) > 5 && !isRespawning)
        {
            transform.Rotate(Vector3.down, torque / SteeringWheel.maxRotation * rotSpeed * Time.deltaTime, Space.Self);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("cannonBall"))
        {
            TakeDamage(1);
        }
    }

    private void TakeDamage(int amount)
    {
        currentHP -= amount;
        Debug.Log($"Ship hit! HP: {currentHP}/{maxHP}");
        PlayDestroyedVFX();
        if (currentHP <= 0 && !isRespawning)
        {
            Debug.Log("Ship destroyed!");
            PlayDestroyedVFX();
            isRespawning = true;
            // Start fade and respawn sequence
            playerRespawn.RespawnWithFade(ResetShip);
        }
    }

    private void PlayDestroyedVFX()
    {
        if (destroyedVFXPrefab != null)
        {
            Instantiate(destroyedVFXPrefab, transform.position, Quaternion.identity);
        }
    }

    // This is now just the reset logic, called from the fade callback
    private void ResetShip()
    {
        currentHP = maxHP;
        transform.position = shipRespawPoint.transform.position;
        // Optionally reset rotation or other states here
        isRespawning = false;
    }
}
