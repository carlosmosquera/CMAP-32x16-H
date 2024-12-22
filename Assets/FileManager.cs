using UnityEngine;
using UnityEngine.UI;
using TMPro;
using extOSC;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using System;

public class FileManager : MonoBehaviour
{
    public GameObject content;
    public Button saveButton;
    public Button loadButton;
    public Button deleteButton;
    public InputField fileNameInput;
    public Dropdown fileDropdown;
    public CustomZoneSpawner customZoneSpawner; // Reference to CustomZoneSpawner
    public Toggle HeadphonesToggle; // Reference to the Toggle UI element
    public OSCTransmitter Transmitter; // Add reference to OSC Transmitter

    private List<Vector3> savedPositions = new List<Vector3>();
    private List<string> savedTexts = new List<string>();
    private List<float> savedAngles = new List<float>(); // To store degree angles
    private string customZoneInputValue = ""; // To store CustomZoneSpawner input field value
    private bool HeadphonesToggleState; // To save the state of the Toggle UI element
    private Transform[] objTransforms;
    private Transform[] textTransforms;
    public Button externalButton; // Reference to the external button

    public Toggle DelayToggle; // Reference to the external delay toggle
    public InputField delayTimeInput; // Reference to the input field for delay time

    private int delayTime; // To store the delay time value as an integer


void Start()
{
    objTransforms = new Transform[transform.childCount];
    for (int i = 0; i < transform.childCount; i++)
    {
        objTransforms[i] = transform.GetChild(i);
    }

    textTransforms = new Transform[content.transform.childCount];
    for (int i = 0; i < content.transform.childCount; i++)
    {
        textTransforms[i] = content.transform.GetChild(i);
    }

    saveButton.onClick.AddListener(SaveData);
    loadButton.onClick.AddListener(LoadSelectedData);
    deleteButton.onClick.AddListener(DeleteSelectedFile);

    UpdateFileDropdown();

    fileDropdown.onValueChanged.AddListener(OnDropdownValueChanged);

    // Load the most recent file
    string lastOpenedFile = PlayerPrefs.GetString("LastOpenedFile", null);
    if (!string.IsNullOrEmpty(lastOpenedFile))
    {
        SelectFileFromDropdown(lastOpenedFile);
        StartCoroutine(InvokeLoadButtonWithDelay());
    }
    else if (fileDropdown.options.Count > 0)
    {
        StartCoroutine(InvokeLoadButtonWithDelay());
    }
}

private void SelectFileFromDropdown(string fileName)
{
    int fileIndex = fileDropdown.options.FindIndex(option => option.text == fileName);
    if (fileIndex != -1)
    {
        fileDropdown.value = fileIndex;
        fileNameInput.text = fileName;
    }
    else
    {
        Debug.LogWarning($"File {fileName} not found in dropdown. Defaulting to the first available file.");
    }
}

private IEnumerator InvokeLoadButtonWithDelay()
{
    yield return new WaitForSeconds(0.3f); // Wait for 3 seconds
    loadButton.onClick.Invoke();
    Debug.Log("Load button clicked programmatically after 3-second delay.");
}

    private void OnDropdownValueChanged(int index)
    {
        fileNameInput.text = fileDropdown.options[index].text;
    }
void SaveData()
{
    string fileName = fileNameInput.text.Trim();
    if (string.IsNullOrEmpty(fileName))
    {
        Debug.LogWarning("File name is empty!");
        return;
    }

    savedPositions.Clear();
    savedTexts.Clear();
    savedAngles.Clear();

    foreach (Transform child in objTransforms)
    {
        savedPositions.Add(child.position);
    }

    foreach (Transform child in textTransforms)
    {
        TMP_InputField inputField = child.GetComponentInChildren<TMP_InputField>();
        if (inputField != null)
        {
            savedTexts.Add(inputField.text);
        }
        else
        {
            savedTexts.Add("");
        }
    }

    savedAngles.AddRange(customZoneSpawner.degreeAngles);
    customZoneInputValue = customZoneSpawner.inputField != null ? customZoneSpawner.inputField.text : "";

    HeadphonesToggleState = HeadphonesToggle != null && HeadphonesToggle.isOn;
    bool delayToggleState = DelayToggle != null && DelayToggle.isOn;

    // Parse delay time as an integer
    if (delayTimeInput != null && int.TryParse(delayTimeInput.text, out int parsedDelayTime))
    {
        delayTime = parsedDelayTime;
    }
    else
    {
        delayTime = 0; // Default value if parsing fails
    }

    SaveDataToFile(fileName, delayToggleState);
    PlayerPrefs.SetString("LastOpenedFile", fileName);
    PlayerPrefs.Save();
    Debug.Log($"Data saved to {fileName}");
    UpdateFileDropdown(fileName);
}

    void LoadSelectedData()
    {
        int selectedIndex = fileDropdown.value;
        string fileName = fileDropdown.options[selectedIndex].text;

        LoadDataFromFile(fileName);
        ApplyLoadedData();
        SendPositionToAllObjects(); // Send OSC messages after loading data

        PlayerPrefs.SetString("LastOpenedFile", fileName);
        PlayerPrefs.Save();
    }

    void DeleteSelectedFile()
    {
        int selectedIndex = fileDropdown.value;
        string fileName = fileDropdown.options[selectedIndex].text;

        string filePath = Path.Combine(Application.persistentDataPath, $"{fileName}.json");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log($"File {fileName} deleted.");
            UpdateFileDropdown();
        }
        else
        {
            Debug.LogWarning($"File {fileName} does not exist!");
        }
    }

private void UpdateFileDropdown(string selectedFileName = null)
{
    fileDropdown.ClearOptions();
    List<string> fileNames = new List<string>();

    string[] files = Directory.GetFiles(Application.persistentDataPath, "*.json");
    foreach (string file in files)
    {
        string fileName = Path.GetFileNameWithoutExtension(file);
        fileNames.Add(fileName);
    }

    fileDropdown.AddOptions(fileNames);

    if (fileNames.Count > 0)
    {
        if (!string.IsNullOrEmpty(selectedFileName) && fileNames.Contains(selectedFileName))
        {
            fileNameInput.text = selectedFileName;
            fileDropdown.value = fileNames.IndexOf(selectedFileName);
        }
        else
        {
            fileNameInput.text = fileNames[0];
            fileDropdown.value = 0;
        }
    }
    else
    {
        fileNameInput.text = "";
    }
}

private void ApplyLoadedData()
{
    if (savedPositions.Count != objTransforms.Length || savedTexts.Count != textTransforms.Length)
    {
        Debug.LogWarning("No saved data or the number of children has changed.");
        return;
    }

    // Apply positions to objects
    for (int i = 0; i < objTransforms.Length; i++)
    {
        objTransforms[i].position = savedPositions[i];
    }

    // Apply texts to input fields
    for (int i = 0; i < textTransforms.Length; i++)
    {
        TMP_InputField inputField = textTransforms[i].GetComponentInChildren<TMP_InputField>();
        if (inputField != null)
        {
            inputField.text = savedTexts[i];
        }
    }

    // Apply degree angles and update input fields
    if (customZoneSpawner != null)
    {
        customZoneSpawner.degreeAngles = new List<float>(savedAngles); // Synchronize angles
        customZoneSpawner.UpdateAngleInputFields(); // Update UI input fields
        customZoneSpawner.SpawnObjects(); // Respawn objects based on new angles
    }

    // Apply other UI states
    if (customZoneSpawner != null && customZoneSpawner.inputField != null)
    {
        customZoneSpawner.inputField.text = customZoneInputValue;
        customZoneSpawner.inputField.onEndEdit.Invoke(customZoneInputValue);
    }

    if (HeadphonesToggle != null)
    {
        HeadphonesToggle.isOn = HeadphonesToggleState;
    }

    if (DelayToggle != null)
    {
        DelayToggle.isOn = HeadphonesToggleState;
    }

    if (delayTimeInput != null)
    {
        delayTimeInput.text = delayTime.ToString();
    }

    Debug.Log("Data loaded and applied.");

    // Trigger external button, if assigned
    if (externalButton != null)
    {
        externalButton.onClick.Invoke();
        Debug.Log("External button was pressed programmatically.");
    }
    else
    {
        Debug.LogWarning("External button is not assigned.");
    }
}

private void SendPositionToAllObjects()
{
    if (Transmitter == null)
    {
        Debug.LogError("OSC Transmitter is not assigned in the Inspector!");
        return;
    }

    StartCoroutine(SendMessagesWithDelay());
}

private IEnumerator SendMessagesWithDelay()
{
    float delay = 0.1f; // Adjust the delay as needed (e.g., 0.1 seconds between messages)

    for (int i = 0; i < objTransforms.Length; i++)
    {
        Transform objTransform = objTransforms[i];
        int objectNumber = i + 1;

        float polarAngle = Mathf.Atan2(-objTransform.position.y, objTransform.position.x) * Mathf.Rad2Deg;
        int normalizedAngle = Mathf.RoundToInt((polarAngle + 90 + 360) % 360);

        var messagePan = new OSCMessage("/objectPosition");
        messagePan.AddValue(OSCValue.Int(objectNumber));
        messagePan.AddValue(OSCValue.Int(normalizedAngle));

        Transmitter.Send(messagePan);
        // Debug.Log($"Sent OSC message: Object {objectNumber}, Angle {normalizedAngle} degrees");

        yield return new WaitForSeconds(delay); // Wait before sending the next message
    }
}



void SaveDataToFile(string fileName, bool delayToggleState)
{
    string filePath = Path.Combine(Application.persistentDataPath, $"{fileName}.json");
    Data data = new Data
    {
        positions = savedPositions.ToArray(),
        texts = savedTexts.ToArray(),
        degreeAngles = savedAngles.ConvertAll(angle => Mathf.RoundToInt(angle)).ToArray(),
        customZoneInputValue = customZoneInputValue,
        headToggleState = HeadphonesToggleState,
        delayToggleState = delayToggleState,
        delayTime = delayTime // Store as an integer
    };
    string jsonData = JsonUtility.ToJson(data);
    File.WriteAllText(filePath, jsonData);
    Debug.Log($"Data saved to {filePath}");
}

void LoadDataFromFile(string fileName)
{
    string filePath = Path.Combine(Application.persistentDataPath, $"{fileName}.json");
    if (File.Exists(filePath))
    {
        string jsonData = File.ReadAllText(filePath);
        Data data = JsonUtility.FromJson<Data>(jsonData);

        // Load data into local variables
        savedTexts = new List<string>(data.texts);
        savedPositions = new List<Vector3>(data.positions);
        savedAngles = new List<float>(Array.ConvertAll(data.degreeAngles, angle => (float)angle));
        customZoneInputValue = data.customZoneInputValue;
        HeadphonesToggleState = data.headToggleState;

        // Ensure all saved angles are applied to CustomZoneSpawner
        if (customZoneSpawner != null)
        {
            // Update the degreeAngles list to match the saved angles
            customZoneSpawner.degreeAngles = new List<float>(savedAngles);

            // Synchronize the number of input fields with the loaded angles
            customZoneSpawner.numberOfObjects = savedAngles.Count;

            // Recreate input fields to match the new degreeAngles list
            customZoneSpawner.CreateAngleInputFields();

            // Update the input fields with the loaded angles
            customZoneSpawner.UpdateAngleInputFields();

            // Spawn objects based on the loaded angles
            customZoneSpawner.SpawnObjects();
        }

        // Apply other UI states
        if (DelayToggle != null)
        {
            DelayToggle.isOn = data.delayToggleState;
        }

        delayTime = data.delayTime;
        if (delayTimeInput != null)
        {
            delayTimeInput.text = delayTime.ToString();
        }

        Debug.Log($"Loaded degree angles: {string.Join(", ", savedAngles)}");
    }
    else
    {
        Debug.LogWarning($"File {fileName} not found!");
    }
}

[System.Serializable]
public class Data
{
    public Vector3[] positions;
    public string[] texts;
    public int[] degreeAngles;
    public string customZoneInputValue;
    public bool headToggleState;
    public bool delayToggleState; // State of the delay toggle
    public int delayTime; // Value of the delay time input field as an integer
}
}