using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using extOSC;

public class CustomZoneSpawner : MonoBehaviour
{
    public GameObject objectPrefab; // The prefab of the object to be spawned
    public int numberOfObjects = 5; // Number of objects to be spawned
    private float radius = 3f; // Radius of the circle

    public List<float> degreeAngles = new List<float>(); // List of degree angles
    private List<GameObject> spawnedObjects = new List<GameObject>(); // List to store spawned objects

    public InputField inputField; // InputField to set numberOfObjects
    public GameObject angleInputPrefab; // Prefab for individual angle input fields
    public Transform angleInputContainer; // Container for angle input fields

    private List<InputField> angleInputFields = new List<InputField>(); // List of angle input fields

    public OSCTransmitter oscTransmitter; // Reference to the OSC transmitter
    public Toggle oscModeToggle; // Toggle to switch between OSC modes
    private string oscAddress2d = "/2d"; // OSC address for default mode
    private string oscAddress2dHeadphones = "/2dHeadphones"; // OSC address for headphones mode

    void Start()
    {
        if (inputField != null)
        {
            inputField.onEndEdit.AddListener(UpdateNumberOfObjects);
        }

        CreateAngleInputFields();
        UpdateDegreeAngles();
        SpawnObjects();
    }

void CreateAngleInputFields()
{
    // Clear existing input fields
    foreach (InputField field in angleInputFields)
    {
        if (field != null)
        {
            Destroy(field.gameObject);
        }
    }
    angleInputFields.Clear();

    // Create new input fields based on degreeAngles
    for (int i = 0; i < degreeAngles.Count; i++)
    {
        GameObject inputGO = Instantiate(angleInputPrefab, angleInputContainer);
        InputField angleField = inputGO.GetComponent<InputField>();
        if (angleField != null)
        {
            int index = i; // Capture the current index for the delegate
            angleField.text = degreeAngles[index].ToString(); // Set to current degree angle
            angleField.onEndEdit.AddListener(value => UpdateAngleAtIndex(index, value));
            angleInputFields.Add(angleField);
        }
    }
}

    void UpdateAngleAtIndex(int index, string value)
    {
        if (float.TryParse(value, out float angle))
        {
            if (index >= degreeAngles.Count)
            {
                degreeAngles.Add(angle);
            }
            else
            {
                degreeAngles[index] = angle;
            }

            SpawnObjects();
        }
        else
        {
            Debug.LogError("Invalid angle input. Please enter a valid number.");
        }
    }

void UpdateDegreeAngles()
{
    degreeAngles.Clear();
    float angleStep = 360f / numberOfObjects;

    for (int i = 0; i < numberOfObjects; i++)
    {
        // Use existing input values or default to the calculated step
        if (i < angleInputFields.Count && float.TryParse(angleInputFields[i].text, out float angle))
        {
            degreeAngles.Add(angle); // Use the input angle directly
        }
        else
        {
            degreeAngles.Add(i * angleStep); // Default to evenly distributed angles
        }
    }
}

void SpawnObjectAtAngle(float angle)
{
    // Negate the angle to reverse direction and make positive clockwise
    float radians = -angle * Mathf.Deg2Rad; // Convert degrees to radians (negative for clockwise)
    float x = Mathf.Cos(radians) * radius; // Calculate x position
    float y = Mathf.Sin(radians) * radius; // Calculate y position

    Vector2 spawnPosition = new Vector2(x, y); // Set the spawn position

    GameObject spawnedObject = Instantiate(objectPrefab, spawnPosition, Quaternion.identity); // Instantiate the object
    spawnedObjects.Add(spawnedObject);
}

public void SpawnObjects()
{
    // Clear previously spawned objects
    foreach (GameObject obj in spawnedObjects)
    {
        if (obj != null)
        {
            Destroy(obj);
        }
    }
    spawnedObjects.Clear();

    // Spawn new objects
    for (int i = 0; i < numberOfObjects; i++)
    {
        float angle = degreeAngles[i] - 90f; // Add 90 degrees to move 0 to the top
        SpawnObjectAtAngle(angle);
    }
}



    public void UpdateNumberOfObjects(string input)
    {
        if (int.TryParse(input, out int newNumber))
        {
            numberOfObjects = Mathf.Max(1, newNumber); // Ensure at least 1 object
            CreateAngleInputFields(); // Recreate input fields
            UpdateDegreeAngles();
            SpawnObjects();
        }
        else
        {
            Debug.LogError("Invalid input. Please enter a valid number.");
        }
    }

    private void OnDestroy()
    {
        if (inputField != null)
        {
            inputField.onEndEdit.RemoveListener(UpdateNumberOfObjects);
        }

        foreach (InputField field in angleInputFields)
        {
            if (field != null)
            {
                field.onEndEdit.RemoveAllListeners();
            }
        }
    }

public void UpdateAngleInputFields()
{
    // Ensure the angle input fields match the current degree angles
    if (angleInputFields.Count != numberOfObjects)
    {
        CreateAngleInputFields(); // Recreate the input fields if count mismatches
    }

    for (int i = 0; i < angleInputFields.Count; i++)
    {
        if (i < degreeAngles.Count)
        {
            angleInputFields[i].text = Mathf.RoundToInt(degreeAngles[i]).ToString(); // Use loaded angles directly
        }
        else
        {
            angleInputFields[i].text = "0"; // Default value if no angle exists
        }
    }

    Debug.Log($"Updated input fields with angles: {string.Join(", ", degreeAngles)}");
}

    public void SendDegreeAnglesViaOSC()
    {
        if (oscTransmitter == null)
        {
            Debug.LogError("OSC Transmitter is not set!");
            return;
        }

        // Determine the OSC address based on toggle state
        string addressToSend = oscModeToggle != null && oscModeToggle.isOn ? oscAddress2dHeadphones : oscAddress2d;

        // Create a new OSC message
        OSCMessage message = new OSCMessage(addressToSend);

        // Add degree angles to the message
        if (oscModeToggle != null && oscModeToggle.isOn)
        {
    // Add a "0" between every degree angle
        for (int i = 0; i < degreeAngles.Count; i++)
         {
        message.AddValue(OSCValue.Int((int)degreeAngles[i])); // Explicitly cast float to int
        message.AddValue(OSCValue.Int(0)); // Add "0" after each angle
             }
        }
        else
{
    foreach (float angle in degreeAngles)
    {
        message.AddValue(OSCValue.Int((int)angle)); // Explicitly cast float to int
    }
}

        // Send the message
        oscTransmitter.Send(message);

        Debug.Log($"Degree angles sent via OSC to {addressToSend}.");
    }
}