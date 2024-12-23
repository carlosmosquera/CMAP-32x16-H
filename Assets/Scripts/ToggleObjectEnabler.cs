using UnityEngine;
using UnityEngine.UI;

public class ToggleObjectEnabler : MonoBehaviour
{
    // Reference to the Toggle component
    public Toggle toggle;

    // Reference to the GameObject to enable/disable
    public GameObject targetObject;

    void Start()
    {
        if (toggle == null || targetObject == null)
        {
            Debug.LogError("Toggle or TargetObject not assigned.");
            return;
        }

        // Subscribe to the Toggle's onValueChanged event
        toggle.onValueChanged.AddListener(OnToggleValueChanged);

        // Set the initial state based on the Toggle's value
        targetObject.SetActive(toggle.isOn);
    }

    void OnToggleValueChanged(bool isOn)
    {
        // Enable or disable the target GameObject based on the Toggle's value
        targetObject.SetActive(isOn);
    }

    void OnDestroy()
    {
        // Unsubscribe from the event to avoid memory leaks
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }
    }
}