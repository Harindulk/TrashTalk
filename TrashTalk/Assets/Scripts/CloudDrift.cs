using UnityEngine;

// Slides a cloud right and wraps it back to minX (local space of its parallax layer).
public class CloudDrift : MonoBehaviour
{
    public float speed = 0.4f, minX = -30f, maxX = 50f;

    void Update()
    {
        var p = transform.localPosition;
        p.x += speed * Time.deltaTime;
        if (p.x > maxX) p.x = minX;
        transform.localPosition = p;
    }
}
