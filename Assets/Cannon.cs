using MathiasCode;
using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.AffordanceSystem.State;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
public class Cannon : MonoBehaviour
{
    PlayerController player;
    public GameObject cannonballPrefab;
    public Transform cannonballSpawnPoint;
    public float fireRateMilli = 1000;
    public float fireSpeed = 2000f;

    private bool allowShoot = true;
    private DateTime nextShoottime = DateTime.MinValue;
    private PIDInteractionManager leftController;
    private PIDInteractionManager rightController;
    private void Awake()
    {
        player = FindObjectOfType<PlayerController>();
    }
    void FixedUpdate()
    {
        if (leftController == null) leftController = InputManager.Instance.GetController(InputManager.ControllerHand.Left);
        if (rightController == null) rightController = InputManager.Instance.GetController(InputManager.ControllerHand.Right);

        if (InputManager.Instance == null) return;

        bool isCarrying = player.isCarrying || rightController.isCarrying || leftController.isCarrying;

        DateTime now = DateTime.Now;
        float fireBtn = InputManager.Instance.GetInput(ControllerButton.TriggerButton, InputManager.ControllerHand.Both);

        if (!allowShoot && fireBtn <= float.Epsilon && nextShoottime < now)
        {
            allowShoot = true;
        }

        if (!allowShoot) return;

        nextShoottime = now + TimeSpan.FromMilliseconds(fireRateMilli);
        Rigidbody rightObj = rightController.carryObj;
        Rigidbody leftObj = leftController.carryObj;
        Rigidbody testPlayerObj = player.carryObj;
        bool isInteractingWithCannon = isCarrying && ((leftObj != null && leftObj.CompareTag("Cannon")) || (rightObj != null && rightObj.CompareTag("Cannon")) || (testPlayerObj != null && testPlayerObj.CompareTag("Cannon")));

        Debug.Log(isInteractingWithCannon);

        if (isInteractingWithCannon && 0 < fireBtn)
        {
            allowShoot = false;
            GameObject ball = Instantiate(cannonballPrefab, cannonballSpawnPoint.position, Quaternion.identity);
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            rb.AddForce(cannonballSpawnPoint.forward * Time.fixedDeltaTime * fireSpeed, ForceMode.Impulse);
        }
    }
}
