using UnityEngine;

public class QuestMarkerFloat : MonoBehaviour
{
    public float floatAmplitude = 50f; // how high it moves
    public float floatFrequency = 100f;    // how fast it moves
    public float scaleSpeed = 2f;        // how fast it scales up
    public Vector3 targetScale;

    private Vector3 startPos;
    private Vector3 initialScale = Vector3.zero;

    void Start()
    {
        targetScale = new Vector3(10f, 10f, 10f); // 👈 force target size
        
        startPos = transform.position;
        transform.localScale = initialScale; // start invisible
    }

    void Update()
    {
        // Floating motion (up and down over time)
        float yOffset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = startPos + new Vector3(0f, yOffset, 0f);

        // Smooth scaling up
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);

        if (Vector3.Distance(transform.localScale, targetScale) < 0.01f)
        {
            transform.localScale = targetScale; // Snap to final scale
        }
    }

}

