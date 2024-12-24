using UnityEngine;
using UnityEngine.UI;

public class EnableChildObjects : MonoBehaviour
{
    public GameObject inputObject; // Assign the first parent object in the inspector
    public GameObject circularObject; // Assign the second parent object in the inspector

    public InputField inputField;   // Assign the input field in the inspector

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
            // Enable/Disable children of inputObject
            EnableChildObjectsForParent(inputObject, numberOfChildrenToEnable);

            // Enable/Disable children of circularObject
            EnableChildObjectsForParent(circularObject, numberOfChildrenToEnable);

            Debug.Log($"{numberOfChildrenToEnable} child objects enabled for each parent.");
        }
        else
        {
            Debug.LogWarning("Invalid input. Please enter a valid number.");
        }
    }

    private void EnableChildObjectsForParent(GameObject parentObject, int numberOfChildrenToEnable)
    {
        // Clamp the value to ensure it doesn't exceed the child count
        numberOfChildrenToEnable = Mathf.Clamp(numberOfChildrenToEnable, 0, parentObject.transform.childCount);

        // Loop through the children and enable or disable them based on the input
        for (int i = 0; i < parentObject.transform.childCount; i++)
        {
            Transform child = parentObject.transform.GetChild(i);
            child.gameObject.SetActive(i < numberOfChildrenToEnable);
        }
    }
}