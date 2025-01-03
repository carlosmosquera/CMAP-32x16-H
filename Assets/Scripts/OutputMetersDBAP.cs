using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using extOSC;

public class OutputMetersDBAP : MonoBehaviour
{
    public OSCReceiver OSCReceiver;

    private readonly string _channelLevelOutBase = "/channelOut/";
    public GameObject sliderPrefab; // Prefab for the slider
    public InputField sliderCountInput; // Input field for user to specify number of sliders
    public Transform sliderContainer; // Parent object to hold the sliders
    public float spacing = 50f; // Space between main sliders
    private List<Slider> channelSliders = new List<Slider>();

    // Start is called before the first frame update
void Start()
{
        sliderCountInput.onEndEdit.AddListener(CreateSliders);

    for (int i = 1; i <= 18; i++)
    {
        int channelIndex = i; // Capture the current index for the closure
        string channel = $"{_channelLevelOutBase}{channelIndex}";
        OSCReceiver.Bind(channel, message => ReceivedOutput(channelIndex, message));
        // Debug.Log($"Bound to OSC channel: {channel}"); // Add this line
    }
}

    // Create sliders dynamically based on user input
// Updated CreateSliders method with binding for extra sliders
private void CreateSliders(string input)
{
    if (int.TryParse(input, out int sliderCount) && sliderCount > 0)
    {
        Debug.Log($"Creating {sliderCount} sliders.");

        // Clear existing sliders
        foreach (Transform child in sliderContainer)
        {
            Destroy(child.gameObject);
        }
        channelSliders.Clear();

        // Create sliders
        for (int i = 0; i < sliderCount; i++)
        {
            GameObject sliderObj = Instantiate(sliderPrefab, sliderContainer);
            sliderObj.SetActive(true);
            sliderObj.transform.localPosition = new Vector3(i * spacing, 0, 0);

            Text labelText = sliderObj.GetComponentInChildren<Text>();
            if (labelText != null)
            {
                labelText.text = $"{i + 1}";
            }

            Slider slider = sliderObj.GetComponent<Slider>();
            if (slider != null)
            {
                channelSliders.Add(slider);
            }
            else
            {
                Debug.LogError("The instantiated object does not have a Slider component.");
            }
        }
    }
    else
    {
        Debug.LogError("Invalid input for slider count.");
    }
}

    // Generic Output Handler with decibel conversion// Generic Output Handler with decibel conversion
public void ReceivedOutput(int channelIndex, OSCMessage message)
{
    // Debug.Log($"Received data for channel {channelIndex}: {message}"); // Debug log for received data

    if (message.ToFloat(out var value))
    {
        if (channelIndex - 1 < channelSliders.Count && channelSliders[channelIndex - 1] != null)
        {
            // Debug.Log($"Attempting to update slider for channel {channelIndex}");

            // Update the corresponding slider
            Slider targetSlider = channelSliders[channelIndex - 1];

            // Convert linear value to decibel scale
            float dBValue = LinearToDecibel(value);

            // Map the dB value to a slider range (e.g., -70 dB to 0 dB => 0.0 to 1.0)
            float normalizedValue = MapDecibelToSlider(dBValue, -70f, 0f);

            // Update the slider value
            targetSlider.value = normalizedValue;

            // Debug to confirm slider update
            // Debug.Log($"Updated slider for channel {channelIndex}: Value: {value}, Normalized: {normalizedValue}, dB: {dBValue}");
        }
        else
        {
            // Debug.LogWarning($"Received data for channel {channelIndex}, but no corresponding slider exists in the list or it's null.");
        }
    }
    else
    {
        Debug.LogError($"Failed to parse OSC message for channel {channelIndex}. Message data: {message}");
    }
}

    // Convert linear amplitude (0.0 to 1.0) to decibels (-∞ to 0 dB)
    private float LinearToDecibel(float linearValue)
    {
        // Avoid logarithm of zero by clamping the minimum value
        linearValue = Mathf.Max(linearValue, 0.0001f);

        // Convert to decibel scale
        return 20f * Mathf.Log10(linearValue);
    }

    // Map decibel values to slider range (e.g., -70 dB to 0 dB -> 0.0 to 1.0)
    private float MapDecibelToSlider(float dBValue, float minDB, float maxDB)
    {
        return Mathf.InverseLerp(minDB, maxDB, dBValue);
    }
}