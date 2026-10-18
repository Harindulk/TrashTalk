using System.Collections;
using UnityEngine;

// Launch pad: brings the next pre-placed rocket onto the pad, slingshot drag to aim, dotted trajectory preview.
public class Launcher : MonoBehaviour
{
    public bool HasRocket;

    [Header("Tuning")]
    public float maxPull = 3.5f;    // world units of drag for full power
    public float cancelPull = 0.3f;
    public float minSpeed = 4f;
    public float maxSpeed = 12f;
    public float dotStep = 0.09f;   // seconds of flight between dots
    public Vector2 rocketOffset = new Vector2(0f, 1.4f); // rocket root above the pad

    [Header("Wiring")]
    public SpriteRenderer[] dots;     // trajectory preview

    Rocket rocket;
    bool ready, dragging;
    public bool Ready => HasRocket && ready;
    public bool Dragging => dragging;
    Vector2 dragStartScreen;
    int lastTick;
    Vector3 dotScale;

    void Awake() => dotScale = dots[0].transform.localScale;

    public void Reload()
    {
        var g = Game.I;
        if (g == null || g.RocketsUsed >= g.Root.rocketsInScene.Count) return;
        rocket = g.Root.rocketsInScene[g.RocketsUsed]; // next unused one in the line
        rocket.transform.SetPositionAndRotation((Vector2)transform.position + rocketOffset, Quaternion.identity);
        rocket.Carry(g.TakeLoad(5));
        HasRocket = true;
        ready = false;
        dragging = false;
        StartCoroutine(DropIn(rocket));
    }

    IEnumerator DropIn(Rocket r)
    {
        Transform v = r.Visual;
        Vector3 basePos = v.localPosition;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.35f)
        {
            v.localPosition = basePos + Vector3.up * 4f * (1f - t * t); // falls in, speeding up
            yield return null;
        }
        v.localPosition = basePos;
        Juice.Punch(v, 0.35f, 0.25f);
        Juice.Burst("dust", transform.position + Vector3.up * 0.5f, new Color(1.00f, 0.96f, 0.87f), 8);
        Sfx.Play("pop");
        ready = true;
    }

    void Update()
    {
        if (!HasRocket || rocket == null || !ready) { HideDots(); return; }
        if (Game.I == null || Game.I.State != GameState.Playing)
        {
            if (dragging) { dragging = false; rocket.CancelAim(); }
            HideDots();
            return;
        }

        if (!dragging && Inp.Pressed)
        {
            dragging = true;
            dragStartScreen = Inp.ScreenPos;
            lastTick = 0;
        }
        if (!dragging) return;

        // Measure the pull in screen space so camera drift can't change the aim.
        var cam = CameraRig.I.Cam;
        float worldPerPixel = cam != null ? 2f * cam.orthographicSize / Screen.height : 0.01f;
        Vector2 pull = (dragStartScreen - Inp.ScreenPos) * worldPerPixel;
        float len = Mathf.Min(pull.magnitude, maxPull);
        float power = len / maxPull;
        Vector2 dir = ClampDir(pull);
        Vector2 vel = dir * Mathf.Lerp(minSpeed, maxSpeed, power);

        rocket.Aim(dir, power);
        if (len >= cancelPull) DrawDots(rocket.transform.position, vel, power);
        else HideDots();

        int tick = Mathf.FloorToInt(power * 4f);
        if (tick > lastTick) Sfx.Play("click", 0.5f, 1f + tick * 0.15f);
        lastTick = tick;

        if (Inp.Released || !Inp.Held)
        {
            dragging = false;
            HideDots();
            if (len < cancelPull) { rocket.CancelAim(); return; }
            HasRocket = false;
            rocket.Launch(vel);
            rocket = null;
        }
    }

    // Keep the launch angle between 10 and 170 degrees (never into the ground).
    static Vector2 ClampDir(Vector2 pull)
    {
        float a = Mathf.Atan2(pull.y, pull.x) * Mathf.Rad2Deg;
        if (a < -90f) a = 170f;
        else if (a < 10f) a = 10f;
        else if (a > 170f) a = 170f;
        float r = a * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
    }

    void DrawDots(Vector2 start, Vector2 vel, float power)
    {
        Vector2 dir = vel.normalized;
        Vector2 p = start, v = vel;
        float phase = (Time.unscaledTime * 2f) % 1f; // dots march forward
        Simulate(ref p, ref v, dir, phase * dotStep);

        for (int i = 0; i < dots.Length; i++)
        {
            var d = dots[i];
            if (p.y < 0f) { d.enabled = false; continue; }
            float k = 1f - (i + phase) / dots.Length;
            d.enabled = true;
            d.transform.position = p;
            d.transform.localScale = dotScale * Mathf.Lerp(0.4f, 1f, k);
            d.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.15f, 0.95f, k));
            Simulate(ref p, ref v, dir, dotStep);
        }
    }

    // Same rules as the real flight: gravity + thrust along the aim + damping.
    void Simulate(ref Vector2 p, ref Vector2 v, Vector2 dir, float time)
    {
        while (time > 0f)
        {
            float h = Mathf.Min(0.02f, time);
            v += (Physics2D.gravity + dir * rocket.Accel) * h;
            v *= 1f / (1f + rocket.damping * h);
            p += v * h;
            time -= h;
        }
    }

    void HideDots()
    {
        for (int i = 0; i < dots.Length; i++) dots[i].enabled = false;
    }
}
