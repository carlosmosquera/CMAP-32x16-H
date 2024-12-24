using UnityEngine;
using UnityEngine.UI;

public class BlinkingButtonController : MonoBehaviour
{
    public Button blinkingButton; // Reference to the button
    public Toggle toggle1, toggle2; // References to the toggles
    public InputField inputField1, inputField2; // References to the input fields

    private bool shouldBlink = false;
    private float blinkInterval = 0.5f; // Blink interval in seconds
    private float blinkTimer = 0.0f;
    private Color defaultColor = Color.white;
    private Color redColor = Color.red;
    private Color greenColor = Color.green;
    private bool isRed = false;

    private void Start()
    {
        // Set up listeners for toggles and input fields
        toggle1.onValueChanged.AddListener(OnTriggerChanged);
        toggle2.onValueChanged.AddListener(OnTriggerChanged);
        inputField1.onValueChanged.AddListener(OnTriggerChanged);
        inputField2.onValueChanged.AddListener(OnTriggerChanged);

        // Set the initial color of the button
        blinkingButton.GetComponent<Image>().color = defaultColor;

        // Set up button click listener
        blinkingButton.onClick.AddListener(OnButtonClicked);
    }

    private void Update()
    {
        if (shouldBlink)
        {
            blinkTimer += Time.deltaTime;

            if (blinkTimer >= blinkInterval)
            {
                ToggleButtonColor();
                blinkTimer = 0.0f;
            }
        }
    }

    private void OnTriggerChanged(bool _)
    {
        StartBlinking();
    }

    private void OnTriggerChanged(string _)
    {
        StartBlinking();
    }

    private void StartBlinking()
    {
        shouldBlink = true;
        blinkTimer = 0.0f; // Reset the timer when starting blinking
    }

    private void ToggleButtonColor()
    {
        var buttonImage = blinkingButton.GetComponent<Image>();
        buttonImage.color = isRed ? defaultColor : redColor;
        isRed = !isRed;
    }

    private void OnButtonClicked()
    {
        shouldBlink = false;
        blinkingButton.GetComponent<Image>().color = greenColor; // Set to green on click
    }
}