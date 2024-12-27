using UnityEngine;
using UnityEngine.UI;
using extOSC;

public class SliderOSCController : MonoBehaviour
{
    private string oscAddress = "/MasterFader";

    [Header("UI Components")]
    public Slider masterFaderSlider;
    public Text valueDisplay;  // Reference to a Text UI component to display the dB value
    public Button SyncButton; // New button reference

    public OSCTransmitter oscTransmitter;

    private float currentDBValue; // Stores the current dB value

    private void Start()
    {
        // Send the initial OSC message with the current slider value
        UpdateFaderValue(masterFaderSlider.value);

        masterFaderSlider.onValueChanged.AddListener(UpdateFaderValue);

        // Add listener for the external button
        if (SyncButton != null)
        {
            SyncButton.onClick.AddListener(SendCurrentDBValue);
        }
    }

    private void UpdateFaderValue(float sliderValue)
    {
        // Map the slider value (0-1) to the dB range (-70 to 0) using a logarithmic scale
        currentDBValue = Mathf.Lerp(-70, 0, Mathf.Log10(1 + 9 * sliderValue) / Mathf.Log10(10));

        // Display the dB value on the canvas
        valueDisplay.text = $"{currentDBValue:F0} dB";

        // Send OSC message with the dB value
        SendOSCMessage(currentDBValue);
    }

    private void SendOSCMessage(float dBValue)
    {
        int dBValueInt = (int)dBValue;

        var message = new OSCMessage(oscAddress);
        message.AddValue(OSCValue.Int(dBValueInt));  // Use Int instead of Float
        oscTransmitter.Send(message);

        // Debug.Log($"Sent OSC Message with dB Value: {dBValueInt}");
    }

    // Method to send the current dB value when the button is pressed
    public void SendCurrentDBValue()
    {
        SendOSCMessage(currentDBValue);
        Debug.Log($"External trigger sent OSC Message with dB Value: {currentDBValue:F0}");
    }

    private void OnDestroy()
    {
        masterFaderSlider.onValueChanged.RemoveListener(UpdateFaderValue);

        // Remove listener for the external button
        if (SyncButton != null)
        {
            SyncButton.onClick.RemoveListener(SendCurrentDBValue);
        }
    }
}