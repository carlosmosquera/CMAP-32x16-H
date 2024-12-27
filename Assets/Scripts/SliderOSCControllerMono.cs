using UnityEngine;
using UnityEngine.UI;
using extOSC;

public class SliderOSCControllerMono : MonoBehaviour
{
    private string oscAddress = "/MonoFader";

    [Header("UI Components")]
    public Slider masterFaderSlider;
    public Text valueDisplay;  // Reference to a Text UI component to display the dB value
    public Button SyncButton; // New button reference

    public OSCTransmitter oscTransmitter;

    private float currentReverbDB; // Stores the current reverb dB value

    private void Start()
    {
        // Send the initial OSC message with the current slider value
        UpdateReverbValue(masterFaderSlider.value);
        masterFaderSlider.onValueChanged.AddListener(UpdateReverbValue);

        // Add listener for the external button
        if (SyncButton != null)
        {
            SyncButton.onClick.AddListener(SendCurrentReverbDB);
        }
    }

    private void UpdateReverbValue(float sliderValue)
    {
        // Map the slider value (0-1) to a dB range (-70 to 0) using a logarithmic scale
        currentReverbDB = Mathf.Lerp(-70, 0, Mathf.Log10(1 + 9 * sliderValue) / Mathf.Log10(10));

        // Display the dB value on the canvas
        valueDisplay.text = $"{currentReverbDB:F0} dB";

        // Send OSC message with the mapped dB value
        SendOSCMessage(currentReverbDB);
    }

    private void SendOSCMessage(float reverbDB)
    {
        int reverbDBInt = (int)reverbDB;

        var message = new OSCMessage(oscAddress);
        message.AddValue(OSCValue.Float(reverbDBInt));
        oscTransmitter.Send(message);

        // Debug.Log($"Sent OSC Message with Reverb dB: {reverbDBInt} dB");
    }

    // Method to send the current reverb dB value when the button is pressed
    public void SendCurrentReverbDB()
    {
        SendOSCMessage(currentReverbDB);
        Debug.Log($"External trigger sent OSC Message with Reverb dB: {currentReverbDB:F0}");
    }

    private void OnDestroy()
    {
        masterFaderSlider.onValueChanged.RemoveListener(UpdateReverbValue);

        // Remove listener for the external button
        if (SyncButton != null)
        {
            SyncButton.onClick.RemoveListener(SendCurrentReverbDB);
        }
    }
}