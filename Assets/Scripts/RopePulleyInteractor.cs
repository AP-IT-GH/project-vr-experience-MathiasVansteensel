using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
public class RopePulleyInteractor : MonoBehaviour
{
    [SerializeField] private Vector3 slideAxis = Vector3.forward;
    [SerializeField] private float minValue = 0;
    [SerializeField] private float maxValue = 100;
    [SerializeField] private float currentValue = 0;
    private float lastValue = 0;
    [SerializeField] private float slideSensitivity = 1.0f;
    [SerializeField] private SharedPulleyValue sharedValue;

    private XRSimpleInteractable simpleInteractable;
    private Vector3 lastHandWorldPosition;
    private Transform activeInteractorTransform;
    private bool isBeingInteracted = false;

    public UnityEvent<float> OnValueChanged;

    void Awake()
    {
        simpleInteractable = GetComponent<XRSimpleInteractable>();

        simpleInteractable.selectEntered.AddListener(OnSelectEntered);
        simpleInteractable.selectExited.AddListener(OnSelectExited);

        if (sharedValue != null)
            sharedValue.OnValueChanged.AddListener(SetValue);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        activeInteractorTransform = args.interactorObject.transform;
        lastHandWorldPosition = activeInteractorTransform.position;
        isBeingInteracted = true;

        if (sharedValue != null)
        {
            currentValue = sharedValue.Value;
            lastValue = currentValue;
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        activeInteractorTransform = null;
        isBeingInteracted = false;
        lastValue = currentValue;
    }

    void Update()
    {
        if (!isBeingInteracted || activeInteractorTransform == null) return;

        // Track hand movement in world space
        Vector3 currentHandPosition = activeInteractorTransform.position;
        Vector3 handMovement = currentHandPosition - lastHandWorldPosition;

        // Convert slide axis to world space (accounts for ship rotation)
        Vector3 worldSlideAxis = transform.TransformDirection(slideAxis.normalized);

        // Project hand movement onto slide axis
        float slideDistance = Vector3.Dot(handMovement, worldSlideAxis);

        // Calculate new value
        float newValue = Mathf.Clamp(
            currentValue + slideDistance * slideSensitivity,
            minValue,
            maxValue
        );

        if (!Mathf.Approximately(currentValue, newValue))
        {
            currentValue = newValue;
            if (sharedValue != null)
                sharedValue.Value = currentValue;
            OnValueChanged.Invoke(currentValue);
        }

        lastHandWorldPosition = currentHandPosition;
    }

    public void SetValue(float newValue)
    {
        currentValue = Mathf.Clamp(newValue, minValue, maxValue);
        OnValueChanged.Invoke(currentValue);
    }

    void OnDrawGizmos()
    {
        Vector3 worldSlideAxis = Application.isPlaying ?
            transform.TransformDirection(slideAxis.normalized) :
            slideAxis.normalized;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, worldSlideAxis * 0.5f);
    }
}
