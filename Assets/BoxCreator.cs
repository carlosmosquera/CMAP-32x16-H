using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BoxCreator : MonoBehaviour
{
    public InputField inputField; // Legacy UI InputField
    public GameObject boxPrefab; // Prefab for the box
    public Transform parent; // Parent object to hold the boxes (optional)
    public float verticalSpacing = 2f; // Space between each box

    private void Start()
    {
        if (inputField != null)
        {
            // Add listeners for input field changes
            inputField.onValueChanged.AddListener(delegate { UpdateBoxes(); });
            inputField.onEndEdit.AddListener(delegate { UpdateBoxes(); });

            // Initial update when the script starts
            UpdateBoxes();
        }
        else
        {
            Debug.LogError("InputField is not assigned.");
        }
    }

    private void UpdateBoxes()
    {
        // Clear existing boxes
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }

        // Parse the input field value
        if (int.TryParse(inputField.text, out int numberOfBoxes))
        {
            for (int i = 1; i <= numberOfBoxes; i++)
            {
                // Instantiate a box
                GameObject box = Instantiate(boxPrefab, parent);

                // Set position (stack vertically, adjust spacing as needed)
                float yPosition = -(i - 1) * verticalSpacing; // Start at 0 and move down
                box.transform.localPosition = new Vector3(0, yPosition, 0);

                // Label the box using TextMeshPro
                TMP_Text text = box.GetComponentInChildren<TMP_Text>();
                if (text != null)
                {
                    text.text = i.ToString(); // Assign progressive numbers
                }
                else
                {
                    Debug.LogWarning($"Box {i} does not have a TMP_Text component.");
                }
            }
        }
        else
        {
            Debug.LogError("Invalid input. Please enter a number.");
        }
    }
}