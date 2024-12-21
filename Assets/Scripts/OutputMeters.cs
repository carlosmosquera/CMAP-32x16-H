using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using extOSC;

public class OutputMeters : MonoBehaviour
{
    public OSCReceiver OSCReceiver;

    private readonly string _channelLevelOutBase = "/channelOut/";
    public GameObject sliderPrefab; // Prefab for the slider
    public InputField sliderCountInput; // Input field for user to specify number of sliders
    public Transform sliderContainer; // Parent object to hold the sliders
    public Toggle extraSlidersToggle; // Toggle for adding two extra sliders
    public float spacing = 50f; // Space between main sliders
    public float extraSpacing = 100f; // Space between extra sliders and the last main slider
    private List<Slider> channelSliders = new List<Slider>();

    // Start is called before the first frame update
    void Start()
    {
        sliderCountInput.onEndEdit.AddListener(CreateSliders);
        if (extraSlidersToggle != null)
        {
            extraSlidersToggle.onValueChanged.AddListener(delegate { CreateSliders(sliderCountInput.text); });
        }

        for (int i = 1; i <= 16; i++)
        {
            int channelIndex = i; // Capture the current index for the closure
            OSCReceiver.Bind($"{_channelLevelOutBase}{channelIndex}", message => ReceivedOutput(channelIndex, message));
        }
    }

    // Create sliders dynamically based on user input
private void CreateSliders(string input)
{
    if (int.TryParse(input, out int sliderCount))
    {
        // Clear existing sliders
        foreach (Transform child in sliderContainer)
        {
            Destroy(child.gameObject);
        }
        channelSliders.Clear();

        // Instantiate main sliders
        for (int i = 0; i < sliderCount; i++)
        {
            // Instantiate slider prefab
            GameObject sliderObj = Instantiate(sliderPrefab, sliderContainer);

            // Ensure the slider object is active
            sliderObj.SetActive(true);

            // Position the slider
            sliderObj.transform.localPosition = new Vector3(i * spacing, 0, 0);

            // Set the label text
            Text labelText = sliderObj.GetComponentInChildren<Text>();
            if (labelText != null)
            {
                labelText.text = $"{i + 1}"; // Number starting from 1 for main sliders
            }

            // Get the Slider component and add it to the list
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

        // Check if the toggle is on
        if (extraSlidersToggle != null && extraSlidersToggle.isOn)
        {
            // Calculate the starting position for the extra sliders
            float extraStartPosition = sliderCount * spacing + extraSpacing;

            // Add two extra sliders
            for (int i = 0; i < 2; i++)
            {
                GameObject extraSliderObj = Instantiate(sliderPrefab, sliderContainer);

                // Ensure the slider object is active
                extraSliderObj.SetActive(true);

                // Position the extra slider
                extraSliderObj.transform.localPosition = new Vector3(extraStartPosition + (i * spacing), 0, 0);

                // Set the label text for extra sliders
                Text labelText = extraSliderObj.GetComponentInChildren<Text>();
                if (labelText != null)
                {
                    labelText.text = $"{sliderCount + i + 1}"; // Continue numbering from the main sliders
                }

                // Get the Slider component and add it to the list
                Slider extraSlider = extraSliderObj.GetComponent<Slider>();
                if (extraSlider != null)
                {
                    channelSliders.Add(extraSlider);
                }
                else
                {
                    Debug.LogError("The instantiated extra object does not have a Slider component.");
                }
            }
        }
    }
    else
    {
        Debug.LogError("Invalid input for slider count.");
    }
}

    // Generic Output Handler with decibel conversion
    public void ReceivedOutput(int channelIndex, OSCMessage message)
    {
        if (message.ToFloat(out var value))
        {
            if (channelIndex - 1 < channelSliders.Count)
            {
                // Convert linear value to decibel scale
                float dBValue = LinearToDecibel(value);

                // Map the dB value to a slider range (e.g., -70 dB to 0 dB => 0.0 to 1.0)
                float normalizedValue = MapDecibelToSlider(dBValue, -70f, 0f);

                // Update the slider value
                channelSliders[channelIndex - 1].value = normalizedValue;
            }
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