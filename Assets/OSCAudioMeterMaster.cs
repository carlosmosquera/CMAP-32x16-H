using UnityEngine;
using UnityEngine.UI;
using extOSC;

public class OSCAudioMeterMaster : MonoBehaviour
{
    [Header("OSC Settings")]
    public OSCReceiver oscReceiver;
    private string address = "/channelOut/20";

    [Header("UI Settings")]
    public Slider audioMeterSlider;

    [Header("Decibel Meter Settings")]
    private float minDecibels = -80f; // Minimum decibels
    private float maxDecibels = 0f;  // Maximum decibels

    [Header("Color Modulation Settings")]
    public Image targetImage; // Image whose color will be changed
    public float colorChangeSpeed = 1f; // Speed of random color change

    private float colorIntensity = 0f;

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

        if (targetImage == null)
        {
            Debug.LogError("Target Image is not assigned.");
            return;
        }

        // Bind OSC Address
        oscReceiver.Bind(address, OnReceivePeakValue);
    }

    private void Update()
    {
        if (targetImage != null)
        {
            // Generate a random color modulated by the slider value
            Color randomColor = new Color(
                Mathf.PerlinNoise(Time.time * colorChangeSpeed, 0f) * colorIntensity,
                Mathf.PerlinNoise(Time.time * colorChangeSpeed, 1f) * colorIntensity,
                Mathf.PerlinNoise(Time.time * colorChangeSpeed, 2f) * colorIntensity
            );

            // Apply the color to the Image component
            targetImage.color = randomColor;
        }
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

            // Use the slider value to set the color intensity
            colorIntensity = normalizedValue;
        }
        else
        {
            Debug.LogWarning("Received OSC message does not contain a valid float value.");
        }
    }
}