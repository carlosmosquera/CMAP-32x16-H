using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using extOSC;

public class CustomZoneSpawner : MonoBehaviour
{
    public GameObject objectPrefab; // The prefab of the object to be spawned
    public int numberOfObjects = 5; // Number of objects to be spawned
    private float radius = 2.8f; // Radius of the circle

    public List<float> degreeAngles = new List<float>(); // List of degree angles
    private List<GameObject> spawnedObjects = new List<GameObject>(); // List to store spawned objects

    public Transform objectContainer; // Container for spawned objects


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

public void CreateAngleInputFields()
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

    // Synchronize numberOfObjects with degreeAngles
    numberOfObjects = degreeAngles.Count; // Ensure numberOfObjects matches the angles list

    // Create new input fields based on degreeAngles
    for (int i = 0; i < degreeAngles.Count; i++)
    {
        GameObject inputGO = Instantiate(angleInputPrefab, angleInputContainer);
        InputField angleField = inputGO.GetComponent<InputField>();
        if (angleField != null)
        {
            int index = i; // Capture the current index for the delegate
            int angleInt = Mathf.Clamp((int)degreeAngles[index], -999, 999); // Ensure 3-character limit
            angleField.contentType = InputField.ContentType.IntegerNumber; // Set input field content type to integer
            angleField.text = angleInt.ToString(); // Set to current degree angle
            angleField.characterLimit = 3; // Limit input to 3 characters

            // Update angle in real-time
            angleField.onValueChanged.AddListener(value =>
            {
                UpdateAngleAtIndex(index, value); // Update angle immediately
                SpawnObjects(); // Refresh spawned objects
            });

            // Validate angle on finishing edit
            angleField.onEndEdit.AddListener(value =>
            {
                UpdateAngleAtIndex(index, value);
                UpdateDegreeAngles();
                SpawnObjects();
            });

            angleInputFields.Add(angleField);
        }
    }
}

void UpdateAngleAtIndex(int index, string value)
{
    if (int.TryParse(value, out int angle))
    {
        angle = Mathf.Clamp(angle, -999, 999); // Restrict angles to 3-character integers
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
    // Ensure degreeAngles matches numberOfObjects
    while (degreeAngles.Count < numberOfObjects)
    {
        degreeAngles.Add(0); // Add default angles
    }

    // Do not remove extra angles to allow proper handling
    if (degreeAngles.Count > numberOfObjects)
    {
        numberOfObjects = degreeAngles.Count; // Update numberOfObjects to match degreeAngles
    }

    // Update degree angles based on input fields
    for (int i = 0; i < degreeAngles.Count; i++)
    {
        if (i < angleInputFields.Count && int.TryParse(angleInputFields[i].text, out int angle))
        {
            angle = Mathf.Clamp(angle, -999, 999); // Restrict to valid angles
            degreeAngles[i] = angle; // Update degree angle list
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

        // Set the parent to the object container if available
        if (objectContainer != null)
        {
            spawnedObject.transform.SetParent(objectContainer);
        }

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
        CreateAngleInputFields(); // Recreate input fields to match new count
        UpdateDegreeAngles(); // Recalculate angles
        SpawnObjects(); // Refresh spawned objects
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
    // Recreate input fields if needed
    if (angleInputFields.Count != degreeAngles.Count)
    {
        CreateAngleInputFields();
    }

    // Sync each input field with its respective angle
    for (int i = 0; i < angleInputFields.Count; i++)
    {
        if (i < degreeAngles.Count)
        {
            int clampedAngle = Mathf.Clamp(Mathf.RoundToInt(degreeAngles[i]), -999, 999); // Restrict to 3-character integers
            angleInputFields[i].text = clampedAngle.ToString(); // Set input field value to the corresponding angle
        }
        else
        {
            angleInputFields[i].text = "0"; // Default value if missing
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
            int clampedAngle = Mathf.Clamp((int)degreeAngles[i], -999, 999); // Restrict to 3-character integers
            message.AddValue(OSCValue.Int(clampedAngle)); // Explicitly cast float to int
            message.AddValue(OSCValue.Int(0)); // Add "0" after each angle
        }
    }
    else
    {
        foreach (float angle in degreeAngles)
        {
            int clampedAngle = Mathf.Clamp((int)angle, -999, 999); // Restrict to 3-character integers
            message.AddValue(OSCValue.Int(clampedAngle)); // Explicitly cast float to int
        }
    }

    // Send the message
    oscTransmitter.Send(message);

    // Debug.Log($"Degree angles sent via OSC to {addressToSend}.");
}
}