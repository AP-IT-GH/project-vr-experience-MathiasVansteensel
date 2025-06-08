using UnityEngine;

public class WheelInteractor : MonoBehaviour
{
    private HingeJoint wheelHingeJoint;
    public float steeringValue;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        wheelHingeJoint = GetComponent<HingeJoint>();
        if (wheelHingeJoint == null)
        {
            Debug.LogError("HingeJoint component not found on " + gameObject.name);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (wheelHingeJoint != null)
        {
            // Get current angle and limits
            float currentAngle = wheelHingeJoint.angle;
            float minLimit = wheelHingeJoint.limits.min;
            float maxLimit = wheelHingeJoint.limits.max;
            
            // Normalize angle from limits to -1 to 1 range
            steeringValue = Mathf.InverseLerp(minLimit, maxLimit, currentAngle) * 2f - 1f;
        }
    }
}
