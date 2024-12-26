using UnityEngine;
using UnityEngine.UI;
using extOSC;

public class OSCAudioMeter : MonoBehaviour
{
    [Header("OSC Settings")]
    public OSCReceiver oscReceiver;
    private string address = "/channelOut/19";

    [Header("UI Settings")]
    public Slider audioMeterSlider;

    [Header("Decibel Meter Settings")]
    private float minDecibels = -70f; // Minimum decibels
    private float maxDecibels = 0f;  // Maximum decibels

    private void Start()
    {
        if (oscReceiver == null)
        {
            Debug.LogError("OSCReceiver is not assigned.");
            return;
        }

        if (audioMeterSlider == null)
        {
            Debug.LogError("AudioMeterSlider is not assigned.");
            return;
        }

        // Bind OSC Address
        oscReceiver.Bind(address, OnReceivePeakValue);
    }

    private void OnReceivePeakValue(OSCMessage message)
    {
        // Ensure the message contains a float value
        if (message.Values.Count > 0 && message.Values[0].Type == OSCValueType.Float)
        {
            float peakValue = message.Values[0].FloatValue;

            // Convert peak value to decibels
            float decibelValue = Mathf.Clamp(20f * Mathf.Log10(Mathf.Max(peakValue, 0.0001f)), minDecibels, maxDecibels);

            // Normalize decibel value to [0, 1] range for the slider
            float normalizedValue = Mathf.InverseLerp(minDecibels, maxDecibels, decibelValue);

            // Update slider value on the UI
            audioMeterSlider.value = normalizedValue;
        }
        else
        {
            Debug.LogWarning("Received OSC message does not contain a valid float value.");
        }
    }
}