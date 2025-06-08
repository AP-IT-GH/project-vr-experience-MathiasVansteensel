using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace MathiasCode 
{
    public class PIDInteractionManager : MonoBehaviour
    {
        

        public PID3DSettings pidSettings;

        PID3D pid;

        public Transform attractor;

        public float interactionRadius = 2f;

        public LayerMask stringInteractionLayer;
        public LayerMask interactionLayer;
        public LayerMask playerLayer;
        private LayerMask mixedInteractionMask;

        public InputManager.ControllerHand hand;

        private bool isNormalCarry = false;
        private bool isStringCarry = false;
        private bool allowCarryInput = true;
        internal bool isCarrying = false;
        internal Rigidbody carryObj;
        internal Collider carryObjCollider;
        private RaycastHit lastHit = default;
        private Vector3 carryObjLocalHitPoint = Vector3.zero;
        private float pickupHoldDistance = 2f;
        private Vector3 carryPoint;
        private float carryObjOriginalAngularDrag = 0f;
        private Vector3 pidProcessValue = Vector3.zero;

        const float Epsilon = 0.0001f;

        void Start()
        {
            InitPID();
            interactionLayer = LayerMask.NameToLayer("Interactable");
            stringInteractionLayer = LayerMask.NameToLayer("Interactable (String)");
            playerLayer = LayerMask.NameToLayer("player");
            mixedInteractionMask = interactionLayer | stringInteractionLayer;
        }

        private void InitPID() 
        {
            pid = new(pidSettings);
        }

        void Update()
        {
            //if (attractVal > 0.1f && Physics.SphereCast(attractor.position, interactionRadius, attractor.forward, out RaycastHit hit, interactionRadius, mixedInteractionMask))
            //{
            //    if (hit.rigidbody == null) return;
            //    bool useStringInteractMode = ((1 << hit.transform.gameObject.layer) & stringInteractionLayer) == stringInteractionLayer;

            //    pid.Setpoint = pid.Setpoint = attractor.position;
            //    pid.ProcessValue = useStringInteractMode ? carryObj.transform.InverseTransformPoint(hit.point) : hit.rigidbody.position;
            //    Vector3 force = pid.Tick(out _, out _, out _, out _) * Time.deltaTime;
            //}


            #region PickupItems
            Vector3 attractorPos = attractor.position;
            Vector3 forwardDir = attractor.transform.forward;
            //bool hitValid = Physics.Raycast(attractorPos, forwardDir, out RaycastHit hit, interactionRadius) && hit.rigidbody is not null && (((1 << hit.rigidbody?.gameObject.layer) & (interactionLayer | stringInteractionLayer)) != 0);
            bool hitValid = Physics.SphereCast(attractor.position, interactionRadius, attractor.forward, out RaycastHit hit, interactionRadius, mixedInteractionMask) && hit.rigidbody is not null && (((1 << hit.rigidbody?.gameObject.layer) & (interactionLayer | stringInteractionLayer)) != 0);
            bool isLookingNotCarrying = !isCarrying && hitValid;
            float grabInput = InputManager.Instance.GetInput(ControllerButton.TriggerButton, hand);
            Debug.Log(grabInput);
            if (allowCarryInput && grabInput > Epsilon)
            {
                
                if (isLookingNotCarrying)
                {
                    carryObj = hit.rigidbody;
                    carryObjCollider = hit.collider;
                    lastHit = hit;
                    int hitObjMask = 1 << hit.rigidbody.gameObject.layer;
                    isNormalCarry = (hitObjMask & interactionLayer) != 0;
                    isStringCarry = (hitObjMask & stringInteractionLayer) != 0;
                    if (isStringCarry) carryObjLocalHitPoint = carryObj.transform.InverseTransformPoint(hit.point);

                    if (carryObj != null && (isNormalCarry || isStringCarry))
                    {
                        //priority for normal carry cuz its more stable
                        pickupHoldDistance = isNormalCarry ? Vector3.Distance(attractorPos, carryObj.position) : hit.distance;
                        //reset pid to stop jerking forces from last pickup
                        InitPID();
                        carryObjOriginalAngularDrag = carryObj.angularDamping;
                        isCarrying = true;
                        carryObj.angularDamping = 3f;
                    }
                }

                carryPoint = attractorPos + (forwardDir * pickupHoldDistance);

                if (isCarrying && carryObj != null && carryObjCollider != null)
                {
                    carryObjCollider.excludeLayers |= playerLayer;

                    pidProcessValue = isNormalCarry ? carryObj.position : carryObj.transform.TransformPoint(carryObjLocalHitPoint);
                    pid.Setpoint = carryPoint;
                    pid.ProcessValue = pidProcessValue;

                    Vector3 correctionForce = pid.Tick(out Vector3 p, out Vector3 i, out Vector3 d, out Vector3 error) * Time.fixedDeltaTime;
                    carryObj.AddForceAtPosition(correctionForce, pidProcessValue);
                }
            }
            else
            {
                if (isCarrying) //one frame after carry release
                {
                    if (carryObjCollider != null && (carryObjCollider.excludeLayers & playerLayer) != 0) carryObjCollider.excludeLayers &= ~playerLayer;
                    if (carryObj != null) carryObj.angularDamping = carryObjOriginalAngularDrag;
                }
                isCarrying = false;

                if (grabInput <= Epsilon) allowCarryInput = true;
            }

            //just as a failsafe
            if (carryObj == null) isCarrying = false;
            #endregion
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(attractor.position + attractor.forward*interactionRadius, interactionRadius);
        }
    }
}
