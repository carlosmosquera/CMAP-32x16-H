using UnityEngine;
using UnityEngine.UI;

public class NumberColumnCreator : MonoBehaviour
{
    public InputField inputField; // Assign your InputField from the UI
    public GameObject numberPrefab; // Assign a prefab for your Text UI element
    public Transform parent; // Assign a parent (e.g., an empty GameObject or a UI panel)

    private void Start()
    {
        // Subscribe to the InputField's onValueChanged event
        inputField.onValueChanged.AddListener(UpdateNumberColumn);
    }

    private void OnDestroy()
    {
        // Unsubscribe from the event to avoid memory leaks
        inputField.onValueChanged.RemoveListener(UpdateNumberColumn);
    }

    private void UpdateNumberColumn(string input)
    {
        // Clear existing numbers
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }

        // Parse the input field value
        if (int.TryParse(input, out int numberOfNumbers))
        {
            int currentNumber = 1; // Start numbering from 1
            for (int i = 1; i <= numberOfNumbers; i++)
            {
                // Instantiate a Text prefab
                GameObject numberObj = Instantiate(numberPrefab, parent);

                // Set its text to the current number
                Text textComponent = numberObj.GetComponent<Text>();
                if (textComponent != null)
                {
                    textComponent.text = currentNumber.ToString();
                }

                // Adjust position (stack vertically)
                RectTransform rectTransform = numberObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = new Vector2(0, -30 * i); // Adjust spacing as needed
                }

                currentNumber++; // Increment the number for the next Text
            }
        }
        else
        {
            Debug.LogError("Invalid input. Please enter a valid number.");
        }
    }
}