using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static MathiasCode.PlayerController;

namespace MathiasCode
{
#warning TODO: rewrite to use triggerproxy
    public class GroundCheck : MonoBehaviour
    {
        internal event Action<bool> OnGroundStateChanged;

        public BoxCollider groundCheckCollider;
        public LayerMask groundLayer;

        internal bool hasInit = false;
        private Vector3 boxcastSize;
        private Transform colliderTransform;

        internal void Init(LayerMask? groundLayer = null, BoxCollider groundCheckCollider = null)
        {
            hasInit = true;
            this.groundCheckCollider = groundCheckCollider ?? GetComponent<BoxCollider>() ?? GetComponentInChildren<BoxCollider>();
            this.groundLayer = groundLayer ?? LayerMask.GetMask("Default");
            colliderTransform = this.groundCheckCollider.transform;
            boxcastSize = this.groundCheckCollider.size / 2f;
        }

        //void Start()
        //{

        //}

        void Update()
        {
            Vector3 castDir = -colliderTransform.up;
            float colliderHeight = groundCheckCollider.size.y;
            ExtDebug.DrawBoxCastBox(transform.position + new Vector3(0, colliderHeight + 0.1f, 0), boxcastSize, colliderTransform.rotation, castDir, 2f * colliderHeight, Color.red);
        }

        private void OnTriggerEnter(Collider other) => CollisionStateChange(other, CollisionState.Enter);

        private void OnTriggerStay(Collider other) => CollisionStateChange(other, CollisionState.Stay);

        private void OnTriggerExit(Collider other) => CollisionStateChange(other, CollisionState.Exit);

        private void OnCollisionEnter(Collision collision) => CollisionStateChange(collision.collider, CollisionState.Enter);

        private void OnCollisionStay(Collision collision) => CollisionStateChange(collision.collider, CollisionState.Stay);

        private void OnCollisionExit(Collision collision) => CollisionStateChange(collision.collider, CollisionState.Exit);

        private void CollisionStateChange(Collider hitObj, CollisionState state)
        {
            bool isGroundObj = ((1 << hitObj.gameObject.layer) & groundLayer.value) != 0;
            if (!isGroundObj || colliderTransform == null) return;

#if DEBUG
            if (!hasInit) throw new Exception("collision detected, but never initialized groundcheck script");
            //if (OnGroundStateChanged is null) throw new Exception("breh the fucking callback is not used.. bitch");
#endif
            switch (state)
            {
                case CollisionState.Enter:
                case CollisionState.Stay:
                    Vector3 castDir = -colliderTransform.up;
                    bool validGround = false;
                    float colliderHeight = groundCheckCollider.size.y;
                    if (Physics.BoxCast(transform.position + new Vector3(0, colliderHeight + 0.1f, 0), boxcastSize, castDir, out RaycastHit hit, colliderTransform.rotation, 2f * colliderHeight, groundLayer))
                    {

                        float groundAngle = Vector3.Dot(castDir, hit.normal);
                        validGround = groundAngle < -0.5f;
                    }
                    OnGroundStateChanged?.Invoke(validGround);
                    return;
                case CollisionState.Exit:
                    OnGroundStateChanged?.Invoke(false);
                    break;
                default:
                    return;
            }
        }
    }
}
