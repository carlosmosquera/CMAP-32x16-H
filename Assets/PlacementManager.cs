using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using extOSC;
using TMPro;

public class PlacementManager : MonoBehaviour
{
    public GameObject prefabToPlace; // Assign the prefab in the Inspector
    public int maxClones = 16;       // Maximum number of prefabs to place
    public Toggle editModeToggle;   // Reference to the external Toggle UI component
    public Toggle deleteModeToggle; // Reference to the external Delete Mode Toggle UI component
    public Button sendButton;       // Reference to the external Button to send data
    public OSCTransmitter oscTransmitter; // Reference to the OSC Transmitter component
    public TMP_Text positionsText;      // Reference to the Text UI for displaying positions
    public InputField maxClonesInputField; // Reference to the InputField for max clones
    public GameObject LoudspeakerContainer;    // External container to hold all prefabs

    private List<Vector2> placedPositions = new List<Vector2>(); // Store positions
    private int currentClones = 0;   // Keep track of placed prefabs
    private BoxCollider2D areaCollider; // 2D Collider for the placement area

    void Start()
    {
        // Ensure the OSC Transmitter is set up
        if (oscTransmitter == null)
        {
            Debug.LogError("OSC Transmitter is not assigned!");
            return;
        }

        // Ensure the Send Button is set up
        if (sendButton == null)
        {
            Debug.LogError("Send Button is not assigned!");
            return;
        }

        // Attach the SendPositions method to the Button's onClick event
        sendButton.onClick.AddListener(SendPositions);

        // Attach an event listener to the input field for updating maxClones
        if (maxClonesInputField != null)
        {
            maxClonesInputField.onValueChanged.AddListener(UpdateMaxClones);
            maxClonesInputField.text = maxClones.ToString(); // Initialize input field with default value
        }
        else
        {
            Debug.LogError("Max Clones InputField is not assigned!");
        }

        // Get the BoxCollider2D component attached to the area
        areaCollider = GetComponent<BoxCollider2D>();
        if (areaCollider == null)
        {
            Debug.LogError("BoxCollider2D is missing!");
        }

        // Ensure the Toggles are assigned
        if (editModeToggle == null)
        {
            Debug.LogError("Edit Mode Toggle is not assigned!");
        }
        if (deleteModeToggle == null)
        {
            Debug.LogError("Delete Mode Toggle is not assigned!");
        }

        // Ensure the Text component is assigned
        if (positionsText == null)
        {
            Debug.LogError("Positions Text is not assigned!");
            return;
        }

        // Ensure the container is assigned or create a new one
        if (LoudspeakerContainer == null)
        {
            LoudspeakerContainer = new GameObject("PrefabContainer");
        }

        // Initial update of the UI
        UpdatePositionsText();
    }

    void Update()
    {
        // Ensure one mode is active at a time
        if (editModeToggle.isOn && deleteModeToggle.isOn)
        {
            Debug.LogWarning("Both Edit and Delete modes are active! Disable one.");
            return;
        }

        if (editModeToggle.isOn)
        {
            HandleEditMode();
        }
        else if (deleteModeToggle.isOn)
        {
            HandleDeleteMode();
        }
    }

    void HandleEditMode()
    {
        if (Input.GetMouseButtonDown(0) && currentClones < maxClones)
        {
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 position = new Vector2(worldPosition.x, worldPosition.y);

            if (IsWithinBounds(position))
            {
                Vector3 spawnPosition = new Vector3(position.x, position.y, 0);
                GameObject newPrefab = Instantiate(prefabToPlace, spawnPosition, Quaternion.identity);

                if (newPrefab != null)
                {
                    // Set the container as the parent of the instantiated prefab
                    newPrefab.transform.SetParent(LoudspeakerContainer.transform);

                    Renderer renderer = newPrefab.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.sortingOrder = 1;
                    }

                    placedPositions.Add(position);
                    currentClones++;

                    // Update UI
                    UpdatePositionsText();
                }
            }
        }
    }

void HandleDeleteMode()
{
    if (Input.GetMouseButtonDown(0))
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 clickPosition = new Vector2(worldPosition.x, worldPosition.y);

        int cloneLayerMask = LayerMask.GetMask("Clone");
        RaycastHit2D hit = Physics2D.Raycast(clickPosition, Vector2.zero, Mathf.Infinity, cloneLayerMask);

        if (hit.collider != null)
        {
            GameObject hitObject = hit.collider.gameObject;

            // Find the position of the prefab being deleted
            Vector2 objectPosition = new Vector2(hitObject.transform.position.x, hitObject.transform.position.y);

            // Use a threshold for comparing positions
            const float positionThreshold = 0.01f;
            Vector2 positionToRemove = Vector2.zero;
            bool found = false;

            foreach (Vector2 position in placedPositions)
            {
                if (Vector2.Distance(position, objectPosition) <= positionThreshold)
                {
                    positionToRemove = position;
                    found = true;
                    break;
                }
            }

            if (found)
            {
                // Remove the position from the list
                placedPositions.Remove(positionToRemove);
            }

            // Destroy the prefab
            Destroy(hitObject);

            // Decrease the current clones count
            currentClones--;

            // Update the positionsText UI
            UpdatePositionsText();
        }
    }
}

    private bool IsWithinBounds(Vector2 position)
    {
        Bounds bounds = areaCollider.bounds;

        return position.x >= bounds.min.x && position.x <= bounds.max.x &&
               position.y >= bounds.min.y && position.y <= bounds.max.y;
    }

    private void UpdatePositionsText()
    {
        if (positionsText != null)
        {
            string displayText = $"Loudspeaker Positions ({placedPositions.Count}):\n";
            for (int i = 0; i < placedPositions.Count; i++)
            {
                Vector2 position = placedPositions[i];
                displayText += $"{i + 1}. ({position.x:F2}, {position.y:F2})\n"; // Add numbering
            }
            positionsText.text = displayText;
        }
    }

    // Send all positions over OSC
    void SendPositions()
    {
        if (placedPositions == null || placedPositions.Count == 0)
        {
            Debug.LogWarning("No positions to send.");
            return;
        }

        // Create an OSC message
        OSCMessage message = new OSCMessage("/DBAPpositions");

        // Iterate through the active positions
        foreach (Vector2 position in placedPositions)
        {
            message.AddValue(OSCValue.Float(position.x));
            message.AddValue(OSCValue.Float(position.y));
        }

        // Send the OSC message
        oscTransmitter.Send(message);

        Debug.Log($"Sent {placedPositions.Count} active positions over OSC.");
    }

    public List<Vector2> GetPlacedPositions()
    {
        return placedPositions;
    }

    // Update the maximum clones allowed from the input field
    void UpdateMaxClones(string inputValue)
    {
        if (int.TryParse(inputValue, out int newMaxClones))
        {
            maxClones = Mathf.Max(0, newMaxClones); // Ensure it's non-negative
            Debug.Log($"Max Clones updated to: {maxClones}");
        }
        else
        {
            Debug.LogError("Invalid input for Max Clones.");
        }
    }
}