using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class RopePulleyInteractor : MonoBehaviour
{
    [SerializeField] private Vector3 slideAxis = Vector3.forward;
    [SerializeField] private float minValue = 0;
    [SerializeField] private float maxValue = 100;
    [SerializeField] private float currentValue = 0;
    private float lastValue = 0;
    [SerializeField] private float slideSensitivity = 1.0f;
    [SerializeField] private SharedPulleyValue sharedValue; // Assign in inspector

    private XRGrabInteractable grabInteractable;
    private Vector3 initialLocalPosition;
    private Transform interactorTransform;

    public UnityEvent<float> OnValueChanged;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.movementType = XRGrabInteractable.MovementType.Kinematic;

        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);

        if (sharedValue != null)
            sharedValue.OnValueChanged.AddListener(SetValue);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        interactorTransform = args.interactorObject.transform;
        initialLocalPosition = transform.InverseTransformPoint(interactorTransform.position);

        // Initialize currentValue and lastValue from the shared value
        if (sharedValue != null)
        {
            currentValue = sharedValue.Value;
            lastValue = currentValue;
        }
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        interactorTransform = null;
        lastValue = currentValue;
    }

    void Update()
    {
        if (!grabInteractable.isSelected) return;

        Vector3 currentPosition = transform.InverseTransformPoint(interactorTransform.position);
        float slideDistance = Vector3.Dot(currentPosition - initialLocalPosition, slideAxis.normalized);

        float newValue = Mathf.Clamp(
            lastValue + slideDistance * slideSensitivity,
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
    }

    public void SetValue(float newValue)
    {
        currentValue = Mathf.Clamp(newValue, minValue, maxValue);
        OnValueChanged.Invoke(currentValue);
    }
}
