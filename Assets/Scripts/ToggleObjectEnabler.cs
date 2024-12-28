using UnityEngine;
using UnityEngine.UI;

public class ToggleObjectEnabler : MonoBehaviour
{
    // Reference to the Toggle component
    public Toggle toggle;

    // References to the GameObjects to enable/disable
    public GameObject[] targetObjects;

    void Start()
    {
        if (toggle == null || targetObjects == null || targetObjects.Length == 0)
        {
            Debug.LogError("Toggle or TargetObjects not assigned or empty.");
            return;
        }

        // Subscribe to the Toggle's onValueChanged event
        toggle.onValueChanged.AddListener(OnToggleValueChanged);

        // Set the initial state for all target objects based on the Toggle's value
        SetTargetObjectsActive(toggle.isOn);
    }

    void OnToggleValueChanged(bool isOn)
    {
        // Enable or disable all target GameObjects based on the Toggle's value
        SetTargetObjectsActive(isOn);
    }

    void SetTargetObjectsActive(bool isActive)
    {
        foreach (GameObject target in targetObjects)
        {
            if (target != null)
            {
                target.SetActive(isActive);
            }
        }
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