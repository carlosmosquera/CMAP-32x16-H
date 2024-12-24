using UnityEngine;
using UnityEngine.UI;

public class IPAddressManager : MonoBehaviour
{
    public InputField ipInputField; // Reference to the InputField for IP address
    private const string IPKey = "SavedIPAddress"; // Key to store and retrieve IP address

    void Start()
    {
        // Load the saved IP address (if exists) when the scene starts
        string savedIP = PlayerPrefs.GetString(IPKey, ""); // Default to an empty string if no IP is saved
        if (!string.IsNullOrEmpty(savedIP))
        {
            ipInputField.text = savedIP;
        }

        // Add a listener to save the IP address whenever it's modified
        ipInputField.onEndEdit.AddListener(SaveIPAddress);
    }

    private void SaveIPAddress(string ipAddress)
    {
        // Save the IP address to PlayerPrefs
        PlayerPrefs.SetString(IPKey, ipAddress.Trim());
        PlayerPrefs.Save();
        // Debug.Log($"IP Address Saved: {ipAddress}");
    }

    void OnApplicationQuit()
    {
        // Optional: Save the current IP address on application quit
        SaveIPAddress(ipInputField.text);
    }
}