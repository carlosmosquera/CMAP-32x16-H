using UnityEngine;
using UnityEngine.UI; // Required for UI components like Toggle

public class ToggleGameObject : MonoBehaviour
{
    [SerializeField] private Toggle toggle; // Reference to the Toggle
    [SerializeField] private GameObject targetGameObject; // The GameObject to enable/disable

    private void Start()
    {
        // Ensure the Toggle has a reference, or get it from the GameObject this script is attached to
        if (toggle == null)
        {
            toggle = GetComponent<Toggle>();
        }

        // Set the initial state of the GameObject
        if (targetGameObject != null && toggle != null)
        {
            targetGameObject.SetActive(toggle.isOn);
        }

        // Subscribe to the Toggle's onValueChanged event
        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }
    }

    private void OnToggleValueChanged(bool isOn)
    {
        // Enable or disable the target GameObject based on the Toggle's state
        if (targetGameObject != null)
        {
            targetGameObject.SetActive(isOn);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from the Toggle's onValueChanged event to avoid memory leaks
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }
    }
}