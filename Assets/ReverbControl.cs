using UnityEngine;
using UnityEngine.UI;
using extOSC;
using TMPro;

public class ReverbControl : MonoBehaviour
{
    public OSCTransmitter Transmitter;

    public TMP_InputField reverbSizeInput; // Input field for size (integer)
    public TMP_InputField reverbDecayInput; // Input field for decay (float)

    private const int minSize = 0;
    private const int maxSize = 100;

    private const float minDecay = 0.0f;
    private const float maxDecay = 1.0f;

    void Start()
    {
        // Add listeners for input field value changes
        reverbSizeInput.onValueChanged.AddListener(OnReverbSizeChanged);
        reverbDecayInput.onValueChanged.AddListener(OnReverbDecayChanged);
    }

    void OnReverbSizeChanged(string input)
    {
        // Validate and send the reverb size (integer)
        if (int.TryParse(input, out int sizeValue))
        {
            sizeValue = Mathf.Clamp(sizeValue, minSize, maxSize);
            SendReverbSize(sizeValue);
        }
        else
        {
            Debug.LogWarning("Invalid reverb size input. Please enter an integer between 0 and 100.");
        }
    }

    void OnReverbDecayChanged(string input)
    {
        // Validate and send the reverb decay (float)
        if (float.TryParse(input, out float decayValue))
        {
            decayValue = Mathf.Clamp(decayValue, minDecay, maxDecay);
            SendReverbDecay(decayValue);
        }
        else
        {
            Debug.LogWarning("Invalid reverb decay input. Please enter a float between 0.0 and 1.0.");
        }
    }

    void SendReverbSize(int sizeValue)
    {
        // Create and send OSC message for reverb size
        var message = new OSCMessage("/reverb/size");
        message.AddValue(OSCValue.Int(sizeValue));
        Transmitter.Send(message);

        Debug.Log($"Sent Reverb Size: {sizeValue}");
    }

    void SendReverbDecay(float decayValue)
    {
        // Create and send OSC message for reverb decay
        var message = new OSCMessage("/reverb/decay");
        message.AddValue(OSCValue.Float(decayValue));
        Transmitter.Send(message);

        Debug.Log($"Sent Reverb Decay: {decayValue}");
    }
}