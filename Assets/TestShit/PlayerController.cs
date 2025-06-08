using UnityEngine;
using Unity.Mathematics;
using System;
using System.Runtime.InteropServices;
using System.Linq;
using UnityEngine.SceneManagement;
using TMPro;

namespace MathiasCode
{
    public enum CollisionState
    {
        Enter,
        Stay,
        Exit
    }

    public class PlayerController : MonoBehaviour
    {
        private const int SPI_GETMOUSESPEED = 0x0070;
        private const float Epsilon = 0.001f;

        private Vector3 prevFrameScale = Vector3.one;

        [Header("Movement")]
        public float jumpForce = 266f;
        public float dragCoeff = 0.6666666666666667f;
        public float sprintMultiplier = 1.66666666666667f;
        public float crouchHeight = 0.5f;
        public float jumpDelaySec = 0.05f;
        public LayerMask groundLayer;
        public float mass = 70f;
        public float playerScale = 1f;

        [Header("Camera")]
        [Range(0.0F, 500.0F)] public float fov = 90f;
        public float sprintFovChange = 10f;
        public Vector2 cameraRotateLimit = new(-90, 90);
        public float speed = 15f;
        public float sensitivity = 8f;
        public float cameraSmoothing = 0.5f;
        public float fovChangeSpeed = 0.01f;
        public Vector3 cameraOffset = Vector3.zero;
        [Space(10)]

        [Header("Interactable")]
        public LayerMask interactableLayer;
        public LayerMask stringInteractableLayer;
        public LayerMask playerLayer;
        public float pickupReach = 4f;
        public float strength = 10000f;
        public float massCarrySlowEffectMultiplier = 10f;
        public LineRenderer lineRenderer;
        [Space(10)]

        [Header("PIDs")]
#warning TODO: use pid3d settings here instead
        public Vector3 proportionalGain = new(10f, 10f, 10f);
        public Vector3 integralGain = Vector3.zero;
        public Vector3 differentialGain = new(5f, 5f, 5f);

        public Vector3 pMax = new(500f, 500f, 500f);
        public Vector3 iMax = Vector3.zero;
        public Vector3 dMax = new(500f, 500f, 500f);
        [Space(10)]

        [Header("Animation/Rig")]
        public float crouchAnimTime = 200;
        public Transform spineBone;
        [Space(10)]

        [Header("Misc")]
        public new Rigidbody rigidbody;
        public Camera fpCamera;
        public Animator animator;
        public GameObject headObj;
        public GroundCheck groundCheckObj;

        public bool EnableCameraInput { get; set; } = true;

#if DEBUG
        //[Header("DEBUG")]
        //public TextMeshProUGUI errorText;
        //public TextMeshProUGUI pForceText;
        //public TextMeshProUGUI iForceText;
        //public TextMeshProUGUI dForceText;
        //public TextMeshProUGUI forceText;
#endif

        private bool isSprinting = false;
        private uint systemMouseSpeed = 1;
        private bool systemMouseSpeedValid = false;
        private float vertAxis;
        private float horizAxis;
        private float verticalCameraAngle = 0f;
        private bool isGrounded = false;
        private bool jumpAllowed = true;
        private float jumpTimePassed = 0f;
        private Vector3 crouchScale;
        private Vector3 originalScale;
        private bool noInput = false;
        internal bool isCarrying = false;
        private Vector3 carryPoint;
        internal Rigidbody carryObj = null;
        private Collider carryObjCollider = null;
        private RaycastHit lastHit = default;
        private PID3D pickupPid;
        private float pickupHoldDistance = 2f;
        private Vector3 carryObjLocalHitPoint = Vector3.zero;
        private float systemMouseSpeedMult = 1;
        private Vector3 pickupPidProcessValue = Vector3.zero;

        public static PlayerController LocalPlayer { get; private set; }

        [DllImport("user32.dll")]
        public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref uint pvParam, uint fWinIni);

        private void Awake()
        {
            LocalPlayer = this;
            InitPID();
            lineRenderer = GetComponentInChildren<LineRenderer>();
            systemMouseSpeedValid = SystemParametersInfo(SPI_GETMOUSESPEED, 0, ref systemMouseSpeed, 0);
            //random "20" here is bc windows uses 20 lvls of mousespeed (via the ui anyway)
            systemMouseSpeedMult = systemMouseSpeed / 20f;
        }

        void Start()
        {
            rigidbody = GetComponent<Rigidbody>();
            fpCamera = GetComponentInChildren<Camera>();
            animator = GetComponent<Animator>();
            interactableLayer = LayerMask.GetMask("Interactable");
            stringInteractableLayer = LayerMask.GetMask("Interactable (string)");
            headObj = GameObject.FindGameObjectsWithTag("PlayerHead").Where(o => o.GetComponentInParent<PlayerController>() == this).FirstOrDefault();
            groundCheckObj = GetComponentInChildren<GroundCheck>();
            groundLayer = LayerMask.GetMask("Default");
            playerLayer = LayerMask.GetMask("Gorilla");
            groundCheckObj.Init(groundLayer);
            Debug.Assert(groundCheckObj.hasInit, "Failed to init groundcheck???????");
            //could be lambda, but fuck u i do whatever i want, also futureproofing in case of upgrades
            groundCheckObj.OnGroundStateChanged += OnGroundStateChanged;
            originalScale = rigidbody.transform.localScale;
            crouchScale = new(originalScale.x, originalScale.y * crouchHeight, originalScale.z);
            fpCamera.fieldOfView = fov;
            Application.onBeforeRender += OnBeforeRender;
            InitPID();
        }


        void InitPID()
        {
            pickupPid = new(proportionalGain, integralGain, differentialGain, pMax, iMax, dMax);
            //UpdatePIDValues();
        }

        //void UpdatePIDValues()
        //{
        //    if (pickupPid.ProportionalGain != proportionalGain) pickupPid.ProportionalGain = proportionalGain;

        //    if (pickupPid.IntegralGain != integralGain) pickupPid.IntegralGain = integralGain;

        //    if (pickupPid.DifferentialGain != differentialGain) pickupPid.DifferentialGain = differentialGain;

        //    if (pickupPid.PMax != pMax) pickupPid.PMax = pMax;

        //    if (pickupPid.PMin != -pMax) pickupPid.PMin = -pMax;

        //    if (pickupPid.IMax != iMax) pickupPid.IMax = iMax;

        //    if (pickupPid.IMin != -iMax) pickupPid.IMin = -iMax;

        //    if (pickupPid.DMax != dMax) pickupPid.DMax = dMax;

        //    if (pickupPid.DMin != -dMax) pickupPid.DMin = -dMax;
        //}

        #region GroundCheck
        private void OnGroundStateChanged(bool grounded) => isGrounded = grounded;
        #endregion

        private bool blockAttack = false;

        private void LateUpdate()
        {
            #region Camera
            float mouseX = EnableCameraInput ? Input.GetAxis("Mouse X") : 0f;
            float mouseY = EnableCameraInput ? -Input.GetAxis("Mouse Y") : 0f;
            Transform camTransform = fpCamera.transform;
            Vector3 localCamOffset = camTransform.InverseTransformVector(cameraOffset);
            float correctedSens = systemMouseSpeedValid ? sensitivity * systemMouseSpeedMult : sensitivity;
            rigidbody.transform.Rotate(rigidbody.transform.up, mouseX * correctedSens);
            verticalCameraAngle = Math.Clamp(mouseY * correctedSens + verticalCameraAngle, cameraRotateLimit.x, cameraRotateLimit.y);
            camTransform.localEulerAngles = new(verticalCameraAngle, camTransform.localEulerAngles.y, camTransform.localEulerAngles.z);
            spineBone.localEulerAngles = new(verticalCameraAngle / 2f, spineBone.localEulerAngles.y, spineBone.localEulerAngles.z);
            Vector3 headTarget = headObj.transform.position + localCamOffset;
            Vector3 camPos = Vector3.MoveTowards(camTransform.position, headTarget, cameraSmoothing * Time.deltaTime);
            //camPos.z = headTarget.z;
            camTransform.position = camPos;
            //Vector3 targetHeadRotation = headObj.transform.rotation.eulerAngles;
            //targetHeadRotation = new(targetHeadRotation.x, targetHeadRotation.y, 0f);
            //Vector3 headrotation = fpCamera.transform.rotation.eulerAngles;
            //fpCamera.transform.rotation = Quaternion.Euler(Vector3.MoveTowards(headrotation, targetHeadRotation, cameraSmoothing));
            #endregion
        }

        private void Update()
        {
            #region Movement
            Vector3 currentScale = rigidbody.transform.localScale;
            Vector3 scaleDerivative = currentScale - prevFrameScale;
            if (Input.GetAxis("Crouch") > Epsilon)
                rigidbody.transform.localScale = Vector3.SmoothDamp(currentScale, crouchScale * playerScale, ref scaleDerivative, crouchAnimTime * Time.deltaTime);
            else if (currentScale != originalScale * playerScale)
                rigidbody.transform.localScale = Vector3.SmoothDamp(rigidbody.transform.localScale * playerScale, originalScale * playerScale, ref scaleDerivative, crouchAnimTime * Time.deltaTime);
            prevFrameScale = currentScale;

            if (!jumpAllowed && isGrounded)
            {
                if (jumpTimePassed >= jumpDelaySec)
                {
                    jumpTimePassed = 0;
                    jumpAllowed = true;
                }
                else jumpTimePassed += Time.deltaTime;
            }

            if (isGrounded && jumpAllowed && (Input.GetAxis("Jump") > Epsilon))
            {
                rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                jumpAllowed = false;
                jumpTimePassed = 0f;
            }

            vertAxis = Input.GetAxisRaw("Vertical");
            horizAxis = Input.GetAxisRaw("Horizontal");

            float sprintAxis = Input.GetAxisRaw("Sprint");
            float sprintMult = sprintAxis * vertAxis > Epsilon ? sprintMultiplier * sprintAxis : 1f;
            isSprinting = sprintMult > Epsilon + 1;

            Vector3 moveDirection =
               rigidbody.transform.forward * vertAxis +
               rigidbody.transform.right * horizAxis;

            rigidbody.AddForce(moveDirection * speed * sprintMult * Time.deltaTime, ForceMode.VelocityChange);
            #endregion

            //Sprint FOV change
            Vector3 velocity = rigidbody.linearVelocity;
            float forwardVelocity = new Vector3(velocity.x * rigidbody.transform.forward.x,
                                                velocity.y * rigidbody.transform.forward.y,
                                                velocity.z * rigidbody.transform.forward.z).magnitude;
            float currentFovLog = Mathf.Log(fpCamera.fieldOfView);
            float targetFovLog = Mathf.Log(fov + (forwardVelocity > Epsilon && sprintMult > (Epsilon + 1) ? sprintFovChange : 0f));
            fpCamera.fieldOfView = Mathf.Exp(Mathf.MoveTowards(currentFovLog, targetFovLog, fovChangeSpeed * Time.deltaTime));

            #region Anim
            bool sprinting = sprintMult > (1f + Epsilon);
            bool strafeDir = horizAxis > 0f;
            float walkingSpeed = velocity.magnitude;
            bool idle = (walkingSpeed < Epsilon && walkingSpeed > -Epsilon) && !sprinting;
            bool strafe = (horizAxis > Epsilon || horizAxis < -Epsilon);

            animator.SetFloat("WalkSpeed", walkingSpeed / 2.1f);
            animator.SetBool("Strafe", strafe);
            animator.SetBool("StrafeDir", strafeDir);
            animator.SetBool("Running", sprinting);
            animator.SetBool("Idle", idle);
            animator.SetBool("Walking", !noInput && isGrounded);

            float attackAxis = Input.GetAxis("Fire1");

            if (blockAttack && attackAxis < Epsilon)
            {
                blockAttack = false;
                animator.ResetTrigger("Punch");
            }

            if (!blockAttack && attackAxis > Epsilon)
            {
                blockAttack = true;
                animator.SetTrigger("Punch");
            }
            #endregion
        }

        private void OnBeforeRender()
        {
            //how, what, why??? why do i have to do this unity...??? wtf
            //ah yes execute code in an instanced class while the instance is null
            //and according to unity's VERY FUCKING HELPFUL errors its somehow my fault
            if (this == null) return;
            if (lineRenderer == null)
            {
                //quite the shitty approach, but... it works i guess
                lineRenderer = GetComponentInChildren<LineRenderer>();
                return;
            }

            //BRO WHAT THE FUCK it just skips the if statement above and gives errors about a null reference when not even in playmode... tf
            //hence the cursed 3x nullcheck
            #region Linerenderer
            if (isCarrying)
            {
                //quite expensive, but unity has no screensize callbacks, even though openGL and directX DO so WHAT THE FUCK
                Vector3 screenCenter = fpCamera.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height / 2f, pickupHoldDistance));
                if (lineRenderer.positionCount != 2) lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, screenCenter);
                lineRenderer.SetPosition(1, pickupPidProcessValue);
            }
            else if (lineRenderer.positionCount > 0) lineRenderer.positionCount = 0;
            #endregion
        }

        //private void OnDrawGizmos()
        //{
        //    Gizmos.DrawSphere(pickupPoint, 0.33f);
        //}

        private float carryObjOriginalAngularDrag = 0f;
        private bool isNormalCarry = false;
        private bool isStringCarry = false;
        private bool allowCarryInput = true;


        void FixedUpdate()
        {
            #region PickupItems
            Vector3 cameraPos = fpCamera.transform.position;
            Vector3 viewDir = fpCamera.transform.forward;
            bool hitValid = Physics.Raycast(cameraPos, viewDir, out RaycastHit hit, pickupReach) && hit.rigidbody is not null && (((1 << hit.rigidbody?.gameObject.layer) & (interactableLayer | stringInteractableLayer)) != 0);
            bool isLookingNotCarrying = !isCarrying && hitValid;
            float grabInput = Input.GetAxis("Fire2");

            if (allowCarryInput && grabInput > Epsilon)
            {
                if (isLookingNotCarrying)
                {
                    carryObj = hit.rigidbody;
                    carryObjCollider = hit.collider;
                    lastHit = hit;
                    int hitObjMask = 1 << hit.rigidbody.gameObject.layer;
                    isNormalCarry = (hitObjMask & interactableLayer) != 0;
                    isStringCarry = (hitObjMask & stringInteractableLayer) != 0;
                    if (isStringCarry) carryObjLocalHitPoint = carryObj.transform.InverseTransformPoint(hit.point);

                    if (carryObj != null && (isNormalCarry || isStringCarry))
                    {
                        //priority for normal carry cuz its more stable
                        pickupHoldDistance = isNormalCarry ? Vector3.Distance(fpCamera.transform.position, carryObj.position) : hit.distance;
                        //reset pid to stop jerking forces from last pickup
                        InitPID();
                        carryObjOriginalAngularDrag = carryObj.angularDamping;
                        isCarrying = true;
                        carryObj.angularDamping = 3f;
                    }
                }

                carryPoint = cameraPos + (viewDir * pickupHoldDistance);

                if (isCarrying && carryObj != null && carryObjCollider != null)
                {
                    carryObjCollider.excludeLayers |= playerLayer;
                    rigidbody.mass = mass + carryObj.mass;
                    rigidbody.linearDamping = carryObj.mass / massCarrySlowEffectMultiplier;

                    pickupPidProcessValue = isNormalCarry ? carryObj.position : carryObj.transform.TransformPoint(carryObjLocalHitPoint);
                    pickupPid.Setpoint = carryPoint;
                    pickupPid.ProcessValue = pickupPidProcessValue;

                    //errorText.text = $"Error: {error}";
                    //float massCorrection = carryObj.mass / 20f;
                    Vector3 correctionForce = pickupPid.Tick(out Vector3 p, out Vector3 i, out Vector3 d, out Vector3 error) * Time.fixedDeltaTime;
                    //TODO: add mass profiles, linear, log, flat(=current)
                    //pForceText.text = $"P-Force: {p}";
                    //iForceText.text = $"I-Force: {i}";
                    //dForceText.text = $"D-Force: {d}";
                    //forceText.text = $"Force (frametime-compensated): {correctionForce}";
                    carryObj.AddForceAtPosition(correctionForce * strength, pickupPidProcessValue);
                }
            }
            else
            {
                if (isCarrying) //one frame after carry release
                {
                    if (rigidbody.linearDamping > Epsilon) rigidbody.linearDamping = 0f;
                    if (carryObjCollider != null && (carryObjCollider.excludeLayers & playerLayer) != 0) carryObjCollider.excludeLayers &= ~playerLayer;
                    if (carryObj is not null) carryObj.angularDamping = carryObjOriginalAngularDrag;
                }
                isCarrying = false;

                if (rigidbody.mass != mass) rigidbody.mass = mass;
                if (grabInput <= Epsilon) allowCarryInput = true;
            }

            //just as a failsafe
            if (carryObj == null) isCarrying = false;
            #endregion

            //artificial drag abonination for x & z axis
            const float StaticDragCoeff = .95f;
            const float DynamicDragCoeff = 0.035f;
            float inverseDynamicDrag = (noInput = (MathF.Abs(vertAxis) + MathF.Abs(horizAxis) <= Epsilon)) ? dragCoeff : 1f;
            //could make another const here for "squared" drag, but the compiler will figure that shit out this "squaring" isnt done every frame because of it
            float totalDrag = inverseDynamicDrag * (isGrounded ? StaticDragCoeff : StaticDragCoeff - DynamicDragCoeff);

            rigidbody.linearVelocity = new(rigidbody.linearVelocity.x * totalDrag, rigidbody.linearVelocity.y, rigidbody.linearVelocity.z * totalDrag);
        }

        public void ReleaseCarryObject() 
        {
            if (!isCarrying) return;
            allowCarryInput = false;
            carryObj = null;
        }

        public void OnFovChange(float fov) 
        {
            this.fov = fov;
            fpCamera.fieldOfView = fov;
        }

        public void OnSensChange(float sens) => sensitivity = sens;
    }
}
