using UnityEngine;
using UnityEngine.UI;

public class PopupManager : MonoBehaviour
{
    public GameObject popupPanel; // Reference to the popup panel
    public Button actionButton;   // Reference to the button

    private void Start()
    {
        // Ensure the popup is hidden at the start
        popupPanel.SetActive(false);

        // Add a listener to the button
        actionButton.onClick.AddListener(ShowPopup);
    }

    private void ShowPopup()
    {
        // Show the popup
        popupPanel.SetActive(true);

        // Start a coroutine to hide the popup after 3 seconds
        StartCoroutine(HidePopupAfterDelay());
    }

    private System.Collections.IEnumerator HidePopupAfterDelay()
    {
        yield return new WaitForSeconds(3); // Wait for 3 seconds
        popupPanel.SetActive(false);       // Hide the popup
    }
}