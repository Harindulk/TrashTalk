using System.Collections.Generic;
using UnityEngine;

// A big fan that pushes every dynamic body inside its wind box. Blows along its local up: rotate it in the Scene view.
public class Fan : MonoBehaviour
{
    public float length = 6f;
    public float strength = 14f;   // acceleration at the fan mouth, fades to half at the far end
    public float width = 2.2f;
    public float spinSpeed = 900f;
    public Transform blades;
    public SpriteRenderer zone;
    public ParticleSystem wind;

    readonly List<Collider2D> hits = new List<Collider2D>();
    readonly HashSet<Rigidbody2D> pushed = new HashSet<Rigidbody2D>();

    // Fit the wind box and particles to length, so a designer only edits one number.
    void Start()
    {
        zone.transform.localPosition = new Vector3(0f, 0.5f + length / 2f, 0f);
        zone.transform.localScale = new Vector3(width, length, 1f);
        var main = wind.main;
        main.startLifetime = length / 7f;   // particles fly at ~7 u/s
        var em = wind.emission;
        em.rateOverTime = 8f + length * 1.5f;
        wind.Play();
    }

    void Update()
    {
        blades.Rotate(0, 0, -spinSpeed * Time.deltaTime);
        zone.color = new Color(1f, 1f, 1f, 0.06f + 0.03f * Mathf.Sin(Time.time * 6f));
    }

    void FixedUpdate()
    {
        Vector2 dir = transform.up, basePos = transform.position;
        Physics2D.OverlapBox(basePos + dir * (0.5f + length / 2f), new Vector2(width, length), transform.eulerAngles.z, ContactFilter2D.noFilter, hits);

        pushed.Clear();
        foreach (var c in hits)
        {
            var body = c.attachedRigidbody;
            if (body == null || body.bodyType != RigidbodyType2D.Dynamic || !pushed.Add(body)) continue;
            float falloff = 1f - 0.5f * Mathf.Clamp01(Vector2.Dot(body.position - basePos, dir) / length);
            body.AddForce(dir * strength * body.mass * falloff);
        }
    }
}
