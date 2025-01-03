using System.Collections.Generic;
using UnityEngine;
using TMPro;
using extOSC;
using UnityEngine.UI;

public class RectangularMovementManager : MonoBehaviour
{
    public OSCTransmitter Transmitter;
    public RectTransform areaPanel; // UI Panel defining the rectangular movement area
    public InputField objectCountInputField; // Input field to set the number of enabled objects

    [System.Serializable]
    public class RectangularObject
    {
        public Transform objectTransform;
        public SpriteRenderer spriteRenderer;

        [HideInInspector]
        public bool isDragging = false;

        [HideInInspector]
        public Transform numberTransform; // For the number attached to the object
        [HideInInspector]
        public TMP_Text numberTextMesh; // Number text mesh for the object
    }

    public List<RectangularObject> rectangularObjects = new List<RectangularObject>();
    private RectTransform panelRectTransform;

    private RectangularObject lastSelectedObject = null; // Track the last selected object

    void Start()
    {
        if (areaPanel == null)
        {
            Debug.LogError("Area Panel is not assigned! Please assign it in the Inspector.");
            return;
        }

        if (objectCountInputField == null)
        {
            Debug.LogError("Object Count Input Field is not assigned! Please assign it in the Inspector.");
            return;
        }

        panelRectTransform = areaPanel.GetComponent<RectTransform>();
        if (panelRectTransform == null)
        {
            Debug.LogError("Area Panel must have a RectTransform component!");
            return;
        }

        objectCountInputField.onValueChanged.AddListener(UpdateActiveObjects);

        // Initialize rectangular objects
        for (int i = 0; i < rectangularObjects.Count; i++)
        {
            RectangularObject obj = rectangularObjects[i];

            // Create and attach number text to each object
            GameObject numberObject = new GameObject("Number");
            numberObject.transform.SetParent(obj.objectTransform);
            numberObject.transform.localPosition = Vector3.zero;

            TMP_Text textMesh = numberObject.AddComponent<TextMeshPro>();
            textMesh.text = (i + 1).ToString();
            textMesh.fontSize = 3;
            textMesh.color = Color.white;
            textMesh.alignment = TextAlignmentOptions.Center;

            // Set sorting order using Renderer component
            Renderer textRenderer = textMesh.GetComponent<Renderer>();
            if (textRenderer != null)
            {
                textRenderer.sortingOrder = 12;
            }

            numberObject.transform.localPosition = new Vector3(0, 0, -1);

            obj.numberTransform = numberObject.transform;
            obj.numberTextMesh = textMesh;

            // Ensure the object has a sprite renderer
            if (obj.spriteRenderer == null)
            {
                obj.spriteRenderer = obj.objectTransform.GetComponent<SpriteRenderer>();
                if (obj.spriteRenderer == null)
                {
                    obj.spriteRenderer = obj.objectTransform.gameObject.AddComponent<SpriteRenderer>();
                }
            }

            obj.spriteRenderer.color = Color.grey; // Default unselected color
            obj.spriteRenderer.sortingOrder = 8;

            // Initially disable all objects
            obj.objectTransform.gameObject.SetActive(false);
        }
    }

    void UpdateActiveObjects(string input)
    {
        if (int.TryParse(input, out int activeCount))
        {
            for (int i = 0; i < rectangularObjects.Count; i++)
            {
                rectangularObjects[i].objectTransform.gameObject.SetActive(i < activeCount);
            }
        }
        else
        {
            Debug.LogError("Invalid input! Please enter a valid number.");
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            RectangularObject topmostObject = null;
            float highestSortingOrder = float.MinValue;

            // Check if any object is clicked
            foreach (var obj in rectangularObjects)
            {
                if (obj.objectTransform.gameObject.activeSelf &&
                    Vector2.Distance(mousePosition, obj.objectTransform.position) < 0.5f &&
                    obj.spriteRenderer.sortingOrder > highestSortingOrder)
                {
                    topmostObject = obj;
                    highestSortingOrder = obj.spriteRenderer.sortingOrder;
                }
            }

            if (topmostObject != null)
            {
                SelectObject(topmostObject);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            foreach (var obj in rectangularObjects)
            {
                if (obj.isDragging)
                {
                    obj.isDragging = false;
                    obj.spriteRenderer.color = obj == lastSelectedObject ? Color.blue : Color.grey;
                }
            }
        }

        if (Input.GetMouseButton(0)) // Dragging logic
        {
            foreach (var obj in rectangularObjects)
            {
                if (obj.isDragging)
                {
                    Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    Vector2 clampedPosition = ClampPositionToPanel(mousePosition);
                    obj.objectTransform.position = clampedPosition;

                    // Send object's position via OSC
                    int objectNumber = rectangularObjects.IndexOf(obj) + 1;
                    var message = new OSCMessage("/objectPositionDBAP/" + objectNumber);
                    // message.AddValue(OSCValue.Int(objectNumber));
                    message.AddValue(OSCValue.Float(clampedPosition.x));
                    message.AddValue(OSCValue.Float(clampedPosition.y));
                    Transmitter.Send(message);
                }
            }
        }
    }

    Vector2 ClampPositionToPanel(Vector2 position)
    {
        Vector3[] panelCorners = new Vector3[4];
        panelRectTransform.GetWorldCorners(panelCorners);

        float left = panelCorners[0].x;
        float right = panelCorners[2].x;
        float bottom = panelCorners[0].y;
        float top = panelCorners[1].y;

        float clampedX = Mathf.Clamp(position.x, left, right);
        float clampedY = Mathf.Clamp(position.y, bottom, top);

        return new Vector2(clampedX, clampedY);
    }

    void SelectObject(RectangularObject obj)
    {
        // Deselect the previous object
        if (lastSelectedObject != null && lastSelectedObject != obj)
        {
            lastSelectedObject.spriteRenderer.color = Color.grey;
            lastSelectedObject.spriteRenderer.sortingOrder = 8;

            if (lastSelectedObject.numberTextMesh != null)
            {
                Renderer numberRenderer = lastSelectedObject.numberTextMesh.GetComponent<Renderer>();
                if (numberRenderer != null)
                {
                    numberRenderer.sortingOrder = 9;
                }
            }
        }

        // Select the new object
        obj.spriteRenderer.color = Color.blue;
        obj.spriteRenderer.sortingOrder = 11;

        if (obj.numberTextMesh != null)
        {
            Renderer numberRenderer = obj.numberTextMesh.GetComponent<Renderer>();
            if (numberRenderer != null)
            {
                numberRenderer.sortingOrder = 12;
            }
        }

        lastSelectedObject = obj;
        obj.isDragging = true;
    }
}