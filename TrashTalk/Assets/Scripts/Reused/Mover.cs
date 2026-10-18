using UnityEngine;

// Ping-pongs between a and b (world space) with smooth easing at the ends.
public class Mover : MonoBehaviour
{
    public Vector2 a, b;
    public float speed = 1f; // average units per second

    float t;
    public Rigidbody2D body; // optional; moves the physics body so trash rides along

    void Start()
    {
        if (body != null) body.bodyType = RigidbodyType2D.Kinematic;
        // Start from wherever we are now so nothing jumps.
        Vector2 ab = b - a;
        if (ab.sqrMagnitude > 0.0001f)
            t = Mathf.Clamp01(Vector2.Dot((Vector2)transform.position - a, ab) / ab.sqrMagnitude);
    }

    void Update()
    {
        if (body != null) return;
        Vector2 p = Step(Time.deltaTime);
        transform.position = new Vector3(p.x, p.y, transform.position.z);
    }

    void FixedUpdate()
    {
        if (body != null) body.MovePosition(Step(Time.fixedDeltaTime));
    }

    Vector2 Step(float dt)
    {
        float dist = Mathf.Max(0.01f, Vector2.Distance(a, b));
        t += dt * speed / dist;
        return Vector2.Lerp(a, b, Mathf.SmoothStep(0f, 1f, Mathf.PingPong(t, 1f)));
    }
}
