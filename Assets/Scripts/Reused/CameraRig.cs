using System.Collections;
using UnityEngine;

// Lives on the Main Camera: smooth follow, intro fly-over, shake, zoom punch. Post-fx is a Volume in the scene.
public class CameraRig : MonoBehaviour
{
    public static CameraRig I;

    public float baseSize = 7f;
    public float followMaxSize = 13f;
    public float followSmooth = 0.25f;
    public float homeSmooth = 0.5f;
    public float sizeSmooth = 0.4f;
    public float lookAhead = 0.35f;
    public float maxLookAhead = 4f;
    public float introDuration = 1.8f;

    public Camera cam;
    Rect bounds;
    bool hasLevel;
    Vector2 home;
    Transform target;
    Rigidbody2D targetBody;
    bool hasTarget;      // stays true after the target dies so we hold on the crash until FollowNone
    Vector2 lastGoal;
    float lastGoalSize;

    Vector2 pos, posVel;
    float size, sizeVel;
    bool intro;
    Coroutine introRoutine;

    float shakeStrength, shakeTime, shakeDuration;
    float punch, punchVel;
    float z;

    void Awake()
    {
        I = this;
        pos = transform.position;
        size = baseSize;
        z = transform.position.z < -0.5f ? transform.position.z : -10f;
        home = pos;
    }

    // ---------- API ----------

    public void SetLevel(Rect bounds, Vector2 home)
    {
        this.bounds = bounds;
        this.home = home;
        hasLevel = true;
        FollowNone(); // drop the last level's rocket, or we'd park on where it crashed
    }

    public void Intro()
    {
        if (introRoutine != null) StopCoroutine(introRoutine);
        introRoutine = StartCoroutine(IntroRoutine());
    }

    public Camera Cam => cam;

    public void Follow(Rigidbody2D body)
    {
        StopIntro();
        Transform t = body != null ? body.transform : null;
        target = t;
        targetBody = body;
        hasTarget = t != null;
        if (t != null) { lastGoal = t.position; lastGoalSize = baseSize; }
    }

    public void FollowNone()
    {
        target = null;
        targetBody = null;
        hasTarget = false;
    }

    public void Shake(float strength, float duration)
    {
        // Keep whichever shake is stronger right now so small shakes don't cancel a big one.
        float current = shakeTime > 0f ? shakeStrength * (shakeTime / shakeDuration) : 0f;
        if (strength < current) return;
        shakeStrength = strength;
        shakeDuration = Mathf.Max(0.01f, duration);
        shakeTime = shakeDuration;
    }

    public void Punch(float zoom)
    {
        punch = Mathf.Clamp(punch + zoom, -2f, 2f);
    }

    // ---------- update ----------

    void LateUpdate()
    {
        float udt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);

        if (!intro)
        {
            Vector2 goal = home;
            float goalSize = baseSize;
            float smooth = homeSmooth;
            if (hasTarget)
            {
                smooth = followSmooth;
                if (target != null)
                {
                    // Keep the ground (and the cans) in view: pan sideways, zoom out to fit the rocket's height.
                    Vector2 v = targetBody != null ? targetBody.linearVelocity : Vector2.zero;
                    Vector2 tp = target.position;
                    float bottom = hasLevel ? bounds.yMin + 0.5f : tp.y - baseSize;
                    lastGoalSize = Mathf.Clamp((tp.y + 3f - bottom) / 2f, baseSize + 0.5f, followMaxSize);
                    lastGoal = new Vector2(tp.x + Mathf.Clamp(v.x * lookAhead, -maxLookAhead, maxLookAhead), bottom + lastGoalSize);
                }
                goal = lastGoal;
                goalSize = lastGoalSize;
            }
            size = Mathf.SmoothDamp(size, goalSize, ref sizeVel, sizeSmooth);
            pos = Vector2.SmoothDamp(pos, Clamp(goal, goalSize), ref posVel, smooth);
        }

        // Zoom punch is a little spring, on unscaled time so it still plays during hit stop.
        punchVel += (-180f * punch - 14f * punchVel) * udt;
        punch += punchVel * udt;

        float finalSize = Mathf.Max(1f, size - punch);
        cam.orthographicSize = finalSize;
        Vector2 p = Clamp(pos, finalSize);

        Vector2 offset = Vector2.zero;
        float roll = 0f;
        if (shakeTime > 0f)
        {
            shakeTime -= udt;
            float k = shakeStrength * Mathf.Clamp01(shakeTime / shakeDuration);
            float n = Time.unscaledTime * 30f;
            offset = new Vector2(Mathf.PerlinNoise(n, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, n) - 0.5f) * 2f * k;
            roll = (Mathf.PerlinNoise(n, n * 0.5f) - 0.5f) * 4f * k;
        }

        transform.position = new Vector3(p.x + offset.x, p.y + offset.y, z);
        transform.rotation = Quaternion.Euler(0f, 0f, roll);
    }

    IEnumerator IntroRoutine()
    {
        intro = true;
        float wide = baseSize;
        if (hasLevel) wide = Mathf.Clamp(bounds.width / (2f * cam.aspect), baseSize, 14f);
        Vector2 from = hasLevel ? Clamp(new Vector2(bounds.center.x, bounds.yMin + wide), wide) : pos;
        Vector2 to = Clamp(home, baseSize);

        pos = from;
        size = wide;
        yield return new WaitForSeconds(0.4f);

        float t = 0f;
        while (t < introDuration)
        {
            t += Time.deltaTime;
            float e = Mathf.SmoothStep(0f, 1f, t / introDuration);
            pos = Vector2.Lerp(from, to, e);
            size = Mathf.Lerp(wide, baseSize, e);
            yield return null;
        }
        posVel = Vector2.zero;
        sizeVel = 0f;
        intro = false;
        introRoutine = null;
    }

    void StopIntro()
    {
        if (introRoutine != null) StopCoroutine(introRoutine);
        introRoutine = null;
        intro = false;
        posVel = Vector2.zero;
        sizeVel = 0f;
    }

    // Keeps the view inside the level bounds (centres on an axis if the level is smaller than the view).
    Vector2 Clamp(Vector2 p, float viewSize)
    {
        if (!hasLevel) return p;
        float h = viewSize, w = viewSize * cam.aspect;
        p.x = bounds.width > 2f * w ? Mathf.Clamp(p.x, bounds.xMin + w, bounds.xMax - w) : bounds.center.x;
        p.y = bounds.height > 2f * h ? Mathf.Clamp(p.y, bounds.yMin + h, bounds.yMax - h) : bounds.yMin + h;
        return p;
    }
}
