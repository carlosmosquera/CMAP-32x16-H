using System.Collections.Generic;
using UnityEngine;
using TMPro;
using extOSC;
using UnityEngine.UI;
using System.Collections;

public class CircularMovementManagerv2 : MonoBehaviour
{
    public OSCTransmitter Transmitter;
    public OSCReceiver Receiver;
    public Toggle cloneVisibilityToggle; // Reference to the Toggle in the UI for showing/hiding clones

    public Toggle movementToggle; // Reference to the toggle in the UI
    public CustomZoneSpawner zoneSpawner; // Reference to CustomZoneSpawner to access degreeAngles

    [System.Serializable]
    public class CircularObject
    {
        public Transform objectTransform;
        public SpriteRenderer spriteRenderer;

        [HideInInspector]
        public bool isDragging = false;
        [HideInInspector]
        public int angle = 0; // Store angle as an integer
        [HideInInspector]
        public float snappedAngle = float.NaN; // Store snapped angle

        [HideInInspector]
        public Transform cloneTransform; // For clone in external area
        [HideInInspector]
        public SpriteRenderer cloneSpriteRenderer; // Renderer for clone
    }

    public List<CircularObject> circularObjects = new List<CircularObject>();
    private List<CircularObject> cloneObjects = new List<CircularObject>();

    public List<TMP_Text> sliderTexts = new List<TMP_Text>(); // Reference to the TMP_Text of each slider
    public Button snapButton; // Reference to the Snap Button

    public Transform slidersParent; // Reference to the parent of all sliders
    private CircularObject lastSelectedObject = null; // Track the last selected object
    private TMP_Text lastSelectedSliderText = null; // Track the last selected slider text

    private float innerRadius = 2.8f;
    private float outerRadius = 3.8f;

    public Transform cloneContainer;


    private IEnumerator DelayedSendPosition()
    {
        yield return new WaitForSeconds(0.5f); // Delay of 0.5 seconds

        if (Transmitter == null)
        {
            Debug.LogError("OSC Transmitter is not assigned in the Inspector!");
            yield break;
        }

        if (circularObjects.Count == 0)
        {
            Debug.LogWarning("CircularObjects list is empty. Nothing to send.");
        }
        else
        {
            SendPosition(); // Call SendPosition after the delay
        }
    }

    void Start()
{
    if (cloneContainer == null)
    {
        Debug.LogError("Clone container is not assigned! Please assign it in the Inspector.");
        return;
    }

    StartCoroutine(DelayedSendPosition());

    for (int i = 0; i < slidersParent.childCount; i++)
    {
        Transform sliderChild = slidersParent.GetChild(i);
        Transform objNumberTransform = sliderChild.Find("Obj Number");
        if (objNumberTransform != null)
        {
            TMP_Text textMeshPro = objNumberTransform.GetComponent<TMP_Text>();
            if (textMeshPro != null)
            {
                sliderTexts.Add(textMeshPro);
                BoxCollider2D collider = objNumberTransform.GetComponent<BoxCollider2D>();
                if (collider == null)
                {
                    collider = objNumberTransform.gameObject.AddComponent<BoxCollider2D>();
                }
                collider.isTrigger = true;
                collider.offset = new Vector2(100, 0);
                collider.size = new Vector2(450, 72);
            }
            else
            {
                Debug.LogWarning($"TextMeshPro component not found in child 'Obj Number' of {sliderChild.name}");
            }
        }
    }

    for (int i = 0; i < circularObjects.Count; i++)
    {
        CircularObject obj = circularObjects[i];

        // Create number object for the main circular object
        GameObject numberObject = new GameObject("Number");
        numberObject.transform.SetParent(obj.objectTransform);
        numberObject.transform.localPosition = Vector3.zero;

        TextMeshPro textMesh = numberObject.AddComponent<TextMeshPro>();
        textMesh.text = (i + 1).ToString();
        textMesh.fontSize = 3;
        textMesh.color = Color.black;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.sortingOrder = 12;

        numberObject.transform.localPosition = new Vector3(0, 0, -1);

        if (obj.spriteRenderer == null)
        {
            obj.spriteRenderer = obj.objectTransform.GetComponent<SpriteRenderer>();
            if (obj.spriteRenderer == null)
            {
                obj.spriteRenderer = obj.objectTransform.gameObject.AddComponent<SpriteRenderer>();
            }
        }

        obj.spriteRenderer.color = Color.grey; // Unselected
        obj.spriteRenderer.sortingOrder = 8;

        // Create clone for the external area
        GameObject clone = new GameObject("Clone");
        clone.transform.SetParent(cloneContainer); // Use the external container instead of 'transform'

        // Position clone based on the outer radius
        Vector3 direction = obj.objectTransform.position.normalized;
        clone.transform.position = direction * outerRadius;

        // Add sprite renderer to the clone and set properties
        var cloneSpriteRenderer = clone.AddComponent<SpriteRenderer>();
        cloneSpriteRenderer.sprite = obj.spriteRenderer.sprite;
        cloneSpriteRenderer.color = new Color(0.5f, 0.8f, 1.0f, 0.8f); // Light blue semi-transparent
        cloneSpriteRenderer.sortingOrder = obj.spriteRenderer.sortingOrder - 1;

        // Scale the clone slightly smaller
        clone.transform.localScale = obj.objectTransform.localScale * 0.8f;

        obj.cloneTransform = clone.transform;

        // Add number text to the clone
        GameObject cloneNumberObject = new GameObject("CloneNumber");
        cloneNumberObject.transform.SetParent(clone.transform);
        cloneNumberObject.transform.localPosition = Vector3.zero;

        TextMeshPro cloneTextMesh = cloneNumberObject.AddComponent<TextMeshPro>();
        cloneTextMesh.text = (i + 1).ToString(); // Match the circular object's number
        cloneTextMesh.fontSize = 2.5f;
        cloneTextMesh.color = Color.black;
        cloneTextMesh.alignment = TextAlignmentOptions.Center;
        cloneTextMesh.sortingOrder = cloneSpriteRenderer.sortingOrder + 1;

        cloneNumberObject.transform.localPosition = new Vector3(0, 0, -1);
    }
    cloneVisibilityToggle.onValueChanged.AddListener(OnCloneVisibilityToggleChanged);
    snapButton.onClick.AddListener(SnapObjectsToClosestAngle);
}

    void OnCloneVisibilityToggleChanged(bool isVisible)
    {
        // Show or hide all clones based on the toggle state
        foreach (var obj in circularObjects)
        {
            if (obj.cloneTransform != null)
            {
                obj.cloneTransform.gameObject.SetActive(isVisible);
            }
        }
    }

    void Update()
{
    if (Input.GetMouseButtonDown(0))
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

        if (hits.Length > 0)
        {
            bool isObjNumberSelected = false;
            CircularObject topmostObject = null;
            float highestSortingOrder = float.MinValue;

            foreach (var hit in hits)
            {
                foreach (var text in sliderTexts)
                {
                    if (hit.collider.transform == text.transform)
                    {
                        SelectSliderText(text);
                        isObjNumberSelected = true;

                        int index = sliderTexts.IndexOf(text);
                        if (index != -1)
                        {
                            HighlightObjectWithoutDragging(circularObjects[index]);
                        }

                        break;
                    }
                }

                if (isObjNumberSelected) break;

                foreach (var obj in circularObjects)
                {
                    if (hit.collider.transform == obj.objectTransform && obj.spriteRenderer.sortingOrder > highestSortingOrder)
                    {
                        topmostObject = obj;
                        highestSortingOrder = obj.spriteRenderer.sortingOrder;
                    }
                }
            }

            if (topmostObject != null)
            {
                SelectObject(topmostObject);
            }
        }
    }

    if (Input.GetMouseButtonUp(0))
    {
        foreach (var obj in circularObjects)
        {
            if (obj.isDragging)
            {
                obj.isDragging = false;
                if (obj == lastSelectedObject)
                {
                    obj.spriteRenderer.color = Color.blue;
                }
                else
                {
                    obj.spriteRenderer.color = new Color(0.2830189f, 0.1081346f, 0.1081346f);
                }
            }
        }
    }

    foreach (var obj in circularObjects)
    {
        if (obj.cloneTransform != null && cloneVisibilityToggle.isOn)
            {
                // Synchronize clone's azimuth with the main object
                Vector3 azimuthDirection = obj.objectTransform.position.normalized;
                obj.cloneTransform.position = azimuthDirection * Mathf.Clamp(obj.cloneTransform.position.magnitude, innerRadius, outerRadius);
            }    
        
        // Handle main object dragging
        if (obj.isDragging)
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            float distanceFromCenter = mousePosition.magnitude;

            float circleRadius = 2.8f;
            if (movementToggle.isOn)
            {
                if (distanceFromCenter > circleRadius)
                {
                    mousePosition = mousePosition.normalized * circleRadius;
                }
            }
            else
            {
                mousePosition = mousePosition.normalized * circleRadius;
            }

            obj.objectTransform.position = mousePosition;

            int elevationPolar = 0;
            if (movementToggle.isOn)
            {
                float elevation = Mathf.Clamp(1 - (distanceFromCenter / circleRadius), 0, 1) * 90;
                elevationPolar = Mathf.RoundToInt(elevation);
            }

            float outPolarFloat = Mathf.Atan2(-obj.objectTransform.position.y, obj.objectTransform.position.x) * Mathf.Rad2Deg;
            int outPolar = Mathf.RoundToInt((outPolarFloat + 90 + 360) % 360);

            int ObjectNumber = circularObjects.IndexOf(obj) + 1;

            var messagePan = new OSCMessage("/objectPosition");
            messagePan.AddValue(OSCValue.Int(ObjectNumber));
            messagePan.AddValue(OSCValue.Int(outPolar));
            if (movementToggle.isOn)
            {
                messagePan.AddValue(OSCValue.Int(elevationPolar));
            }

            Transmitter.Send(messagePan);
        }

        // Handle clone behavior
// Handle clone behavior
   if (obj.cloneTransform != null)
    {
        // Synchronize clone's azimuth with the main object
        Vector3 azimuthDirection = obj.objectTransform.position.normalized;
        obj.cloneTransform.position = azimuthDirection * Mathf.Clamp(obj.cloneTransform.position.magnitude, innerRadius, outerRadius);

        // Allow dragging clones within the external area
        if (Input.GetMouseButton(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // Check if mouse position is near the clone to allow dragging
            if (Vector2.Distance(mousePosition, obj.cloneTransform.position) < 0.5f)
            {
                // Drag the clone
                float distance = Mathf.Clamp(mousePosition.magnitude, innerRadius, outerRadius);
                Vector3 newPosition = azimuthDirection * distance;

                // Check if the position has changed
                if (obj.cloneTransform.position != newPosition)
                {
                    obj.cloneTransform.position = newPosition;

                    // Calculate normalized value (0.0 at innerRadius, 1.0 at outerRadius)
                    float normalizedValue = Mathf.InverseLerp(innerRadius, outerRadius, distance);

                    // Send OSC message
                    int cloneNumber = circularObjects.IndexOf(obj) + 1;
                    string oscAddress = $"/revsend/{cloneNumber}";

                    var cloneMessage = new OSCMessage(oscAddress);
                    cloneMessage.AddValue(OSCValue.Float(normalizedValue));

                    Transmitter.Send(cloneMessage);
                }
            }
        }
        }   
    }
}

    void SnapObjectsToClosestAngle()
    {
        if (lastSelectedObject != null)
        {
            float outPolarFloat = Mathf.Atan2(-lastSelectedObject.objectTransform.position.y, lastSelectedObject.objectTransform.position.x) * Mathf.Rad2Deg;
            float currentAngle = (outPolarFloat + 90 + 360) % 360;

            float closestAngle = float.NaN;
            float minDifference = float.MaxValue;

            foreach (float angle in zoneSpawner.degreeAngles)
            {
                float difference = Mathf.Abs(Mathf.DeltaAngle(currentAngle, angle));
                if (difference < minDifference)
                {
                    minDifference = difference;
                    closestAngle = angle;
                }
            }

            lastSelectedObject.snappedAngle = closestAngle;

            float radiansClosest = (closestAngle - 90) * Mathf.Deg2Rad;
            float xClosest = Mathf.Cos(radiansClosest) * 2.8f;
            float yClosest = -Mathf.Sin(radiansClosest) * 2.8f;

            lastSelectedObject.objectTransform.position = new Vector2(xClosest, yClosest);

            int ObjectNumberSnapped = circularObjects.IndexOf(lastSelectedObject) + 1;

            var messagePan = new OSCMessage("/objectPosition");
            messagePan.AddValue(OSCValue.Int(ObjectNumberSnapped));
            messagePan.AddValue(OSCValue.Float(closestAngle));

            Transmitter.Send(messagePan);
        }
    }

    void SelectObject(CircularObject obj)
    {
        if (lastSelectedObject != null && lastSelectedObject != obj)
        {
            lastSelectedObject.spriteRenderer.color = Color.grey;
            lastSelectedObject.spriteRenderer.sortingOrder = 8;

            Transform numberTransform = lastSelectedObject.objectTransform.Find("Number");
            if (numberTransform != null)
            {
                var textMeshPro = numberTransform.GetComponent<TextMeshPro>();
                if (textMeshPro != null)
                {
                    textMeshPro.color = Color.black;
                    textMeshPro.sortingOrder = 9;
                }
            }
        }

        obj.spriteRenderer.color = Color.blue;
        obj.spriteRenderer.sortingOrder = 11;

        Transform currentNumberTransform = obj.objectTransform.Find("Number");
        if (currentNumberTransform != null)
        {
            var currentTextMeshPro = currentNumberTransform.GetComponent<TextMeshPro>();
            if (currentTextMeshPro != null)
            {
                currentTextMeshPro.color = Color.white;
                currentTextMeshPro.sortingOrder = 12;
            }
        }

        int objIndex = circularObjects.IndexOf(obj);
        if (objIndex != -1 && objIndex < sliderTexts.Count)
        {
            SelectSliderText(sliderTexts[objIndex]);
        }

        lastSelectedObject = obj;
        obj.isDragging = true;
    }

    void SelectSliderText(TMP_Text text)
    {
        int index = sliderTexts.IndexOf(text);
        if (index != -1)
        {
            if (lastSelectedSliderText != null)
            {
                lastSelectedSliderText.color = Color.white;
            }

            lastSelectedSliderText = sliderTexts[index];
            lastSelectedSliderText.color = Color.blue;

            if (index < circularObjects.Count)
            {
                HighlightObjectWithoutDragging(circularObjects[index]);
            }
        }
        else
        {
            Debug.LogWarning($"Index {index} is invalid or not found in sliderTexts");
        }
    }

    void HighlightObjectWithoutDragging(CircularObject obj)
    {
        if (lastSelectedObject != null && lastSelectedObject != obj)
        {
            lastSelectedObject.spriteRenderer.color = Color.grey;
            lastSelectedObject.spriteRenderer.sortingOrder = 8;

            Transform numberTransform = lastSelectedObject.objectTransform.Find("Number");
            if (numberTransform != null)
            {
                var textMeshPro = numberTransform.GetComponent<TextMeshPro>();
                if (textMeshPro != null)
                {
                    textMeshPro.color = Color.black;
                    textMeshPro.sortingOrder = 9;
                }
            }
        }

        obj.spriteRenderer.color = Color.blue;
        obj.spriteRenderer.sortingOrder = 11;

        Transform currentNumberTransform = obj.objectTransform.Find("Number");
        if (currentNumberTransform != null)
        {
            var currentTextMeshPro = currentNumberTransform.GetComponent<TextMeshPro>();
            if (currentTextMeshPro != null)
            {
                currentTextMeshPro.color = Color.white;
                currentTextMeshPro.sortingOrder = 12;
            }
        }

        lastSelectedObject = obj;
    }

    void SendPosition()
    {
        foreach (var obj in circularObjects)
        {
            float outPolarFloat = Mathf.Atan2(-obj.objectTransform.position.y, obj.objectTransform.position.x) * Mathf.Rad2Deg;
            int outPolar = Mathf.RoundToInt((outPolarFloat + 90 + 360) % 360);

            int ObjectNumber = circularObjects.IndexOf(obj) + 1;

            var messagePan = new OSCMessage("/objectPosition");
            messagePan.AddValue(OSCValue.Int(ObjectNumber));
            messagePan.AddValue(OSCValue.Int(outPolar));

            Transmitter.Send(messagePan);
        }
    }
}