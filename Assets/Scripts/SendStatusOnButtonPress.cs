using UnityEngine;
using UnityEngine.UI;
using extOSC;

public class SendOSCOnButtonPress : MonoBehaviour
{
    public OSCTransmitter oscTransmitter; // Reference to the OSC Transmitter
    public Toggle statusToggle; // Reference to the Toggle UI element
    public InputField numberInputField; // Reference to the InputField UI element
    public Button sendButton; // Reference to the Button UI element

    private string toggleOSCAddress = "/delay/toggle/";
    private string numberOSCAddress = "/delay/number/";

    void Start()
    {
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendButtonPressed);
        }

        if (oscTransmitter == null)
        {
            Debug.LogError("OSC Transmitter is not assigned!");
        }
    }

    void OnSendButtonPressed()
    {
        if (oscTransmitter == null)
        {
            Debug.LogError("OSC Transmitter is not assigned!");
            return;
        }

        if (statusToggle == null || numberInputField == null)
        {
            Debug.LogError("Toggle or InputField is not assigned!");
            return;
        }

        // Send the toggle status
        bool isToggleOn = statusToggle.isOn;
        var toggleMessage = new OSCMessage(toggleOSCAddress);
        toggleMessage.AddValue(OSCValue.Bool(isToggleOn));
        oscTransmitter.Send(toggleMessage);
        // Debug.Log($"Sent OSC Message: {toggleOSCAddress} {isToggleOn}");

        // Send the number from the input field
        if (int.TryParse(numberInputField.text, out int number))
        {
            var numberMessage = new OSCMessage(numberOSCAddress);
            numberMessage.AddValue(OSCValue.Int(number));
            oscTransmitter.Send(numberMessage);
            // Debug.Log($"Sent OSC Message: {numberOSCAddress} {number}");
        }
        else
        {
            Debug.LogWarning("Invalid number in InputField!");
        }
    }
}