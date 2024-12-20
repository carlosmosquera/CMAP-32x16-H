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
    public OSCTransmitter Transmitter;

    public CustomZoneSpawner customZoneSpawner; // Reference to CustomZoneSpawner
    public Toggle toggle; // Reference to the Toggle UI element

    private List<Vector3> savedPositions = new List<Vector3>();
    private List<string> savedTexts = new List<string>();
    private List<float> savedAngles = new List<float>(); // To store degree angles
    private string customZoneInputValue = ""; // To store CustomZoneSpawner input field value
    private bool toggleState; // To save the state of the Toggle UI element
    private Transform[] objTransforms;
    private Transform[] textTransforms;
    public Button externalButton; // Reference to the external button

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

        // Simulate clicking the Load button at the start
    if (loadButton != null)
    {
        loadButton.onClick.Invoke();
        Debug.Log("Load button clicked programmatically at startup.");
    }
    else
    {
        Debug.LogWarning("Load button is not assigned.");
    }
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

    toggleState = toggle != null && toggle.isOn;

    SaveDataToFile(fileName);
    PlayerPrefs.SetString("LastOpenedFile", fileName); // Save the most recent file name
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

    PlayerPrefs.SetString("LastOpenedFile", fileName); // Update last opened file
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
        Debug.LogWarning("No saved data or number of children has changed");
        return;
    }

    for (int i = 0; i < objTransforms.Length; i++)
    {
        objTransforms[i].position = savedPositions[i];
    }

    for (int i = 0; i < textTransforms.Length; i++)
    {
        TMP_InputField inputField = textTransforms[i].GetComponentInChildren<TMP_InputField>();
        if (inputField != null)
        {
            inputField.text = savedTexts[i];
        }
    }

    // Apply loaded angles to CustomZoneSpawner
    customZoneSpawner.degreeAngles = new List<float>(savedAngles); // Assign loaded angles directly
    Debug.Log($"Applying degree angles: {string.Join(", ", savedAngles)}");

    if (customZoneSpawner.inputField != null)
    {
        customZoneSpawner.inputField.text = customZoneInputValue;
        customZoneSpawner.inputField.onEndEdit.Invoke(customZoneInputValue);
    }

    // Refresh input fields and objects
    customZoneSpawner.UpdateAngleInputFields();
    customZoneSpawner.SpawnObjects();

    // Apply toggle state
    if (toggle != null)
    {
        toggle.isOn = toggleState;
    }

    Debug.Log("Data loaded and applied");

    // Invoke the external button's onClick
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

    private IEnumerator SendPositionsViaOSC()
    {
        for (int i = 0; i < savedPositions.Count; i++)
        {
            float outPolarFloat = Mathf.Atan2(savedPositions[i].y, savedPositions[i].x) * Mathf.Rad2Deg;
            int outPolar = Mathf.RoundToInt((450 - outPolarFloat) % 360);
            int objectNumber = i + 1;

            var message = new OSCMessage("/objectPosition");
            message.AddValue(OSCValue.Int(objectNumber));
            message.AddValue(OSCValue.Int(outPolar));

            Transmitter.Send(message);
            yield return new WaitForSeconds(0.1f);
        }
    }

void SaveDataToFile(string fileName)
{
    string filePath = Path.Combine(Application.persistentDataPath, $"{fileName}.json");
    Data data = new Data
    {
        positions = savedPositions.ToArray(),
        texts = savedTexts.ToArray(),
        degreeAngles = savedAngles.ConvertAll(angle => Mathf.RoundToInt(angle)).ToArray(), // Convert floats to integers
        customZoneInputValue = customZoneInputValue,
        toggleState = toggleState
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

        savedTexts = new List<string>(data.texts);
        savedPositions = new List<Vector3>(data.positions);
        savedAngles = new List<float>(Array.ConvertAll(data.degreeAngles, angle => (float)angle)); // Convert integers to floats for internal use
        customZoneInputValue = data.customZoneInputValue;
        toggleState = data.toggleState;

        Debug.Log($"Loaded degree angles: {string.Join(", ", data.degreeAngles)}");
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
        public bool toggleState; // State of the toggle
    }
}