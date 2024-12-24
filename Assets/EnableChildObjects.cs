using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnableChildObjects : MonoBehaviour
{
    public GameObject parentObject; // Assign the parent object in the inspector
    public TMP_InputField inputField;   // Assign the input field in the inspector

    private void Start()
    {
        // Attach listener to handle input field changes
        inputField.onEndEdit.AddListener(EnableChildren);
    }

    private void EnableChildren(string input)
    {
        // Parse input value
        if (int.TryParse(input, out int numberOfChildrenToEnable))
        {
            // Clamp the value to ensure it doesn't exceed the child count
            numberOfChildrenToEnable = Mathf.Clamp(numberOfChildrenToEnable, 0, parentObject.transform.childCount);

            // Loop through the children and enable or disable them based on the input
            for (int i = 0; i < parentObject.transform.childCount; i++)
            {
                Transform child = parentObject.transform.GetChild(i);
                child.gameObject.SetActive(i < numberOfChildrenToEnable);
            }

            Debug.Log($"{numberOfChildrenToEnable} child objects enabled.");
        }
        else
        {
            Debug.LogWarning("Invalid input. Please enter a valid number.");
        }
    }
}