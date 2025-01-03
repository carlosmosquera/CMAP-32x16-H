using UnityEngine;
using UnityEngine.UI;
using extOSC;

public class EngineControl : MonoBehaviour
{
    [Header("OSC Settings")]
    public OSCTransmitter transmitter; // Assign your OSC transmitter here in the Unity Editor
    public string oscAddress = "/Engine"; // The OSC address to send messages to

    [Header("Toggles")]
    public Toggle toggle1; // First toggle
    public Toggle toggle2; // Second toggle

    private void Start()
    {
        // Ensure both toggles are assigned
        if (toggle1 == null || toggle2 == null)
        {
            Debug.LogError("Toggles are not assigned.");
            return;
        }

        // Ensure the transmitter is assigned
        if (transmitter == null)
        {
            Debug.LogError("OSC Transmitter is not assigned.");
            return;
        }

        // Add listeners to toggles
        toggle1.onValueChanged.AddListener((isOn) => OnToggleChanged(toggle1, 1, isOn));
        toggle2.onValueChanged.AddListener((isOn) => OnToggleChanged(toggle2, 2, isOn));
    }

    private void OnToggleChanged(Toggle toggle, int argument, bool isOn)
    {
        if (isOn)
        {
            // Make sure the other toggle is off
            if (toggle == toggle1 && toggle2.isOn)
            {
                toggle2.isOn = false;
            }
            else if (toggle == toggle2 && toggle1.isOn)
            {
                toggle1.isOn = false;
            }

            // Send the OSC message
            SendOSCMessage(argument);
        }
    }

    private void SendOSCMessage(int argument)
    {
        var message = new OSCMessage(oscAddress);
        message.AddValue(OSCValue.Int(argument)); // Add the integer argument to the message
        transmitter.Send(message); // Send the OSC message

        Debug.Log($"Sent OSC message to {oscAddress} with argument {argument}");
    }
}