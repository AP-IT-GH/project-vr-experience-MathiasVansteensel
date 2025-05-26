using UnityEngine;
using UnityEngine.Events;

public class SharedPulleyValue : MonoBehaviour
{
    [SerializeField] private float value = 0f;
    public UnityEvent<float> OnValueChanged;

    public float Value
    {
        get => value;
        set
        {
            if (Mathf.Approximately(this.value, value)) return;
            this.value = value;
            OnValueChanged?.Invoke(this.value);
        }
    }
}