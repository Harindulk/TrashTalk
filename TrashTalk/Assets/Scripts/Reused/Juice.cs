using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Screen shake, hit stop, particle bursts, popups and squash punches. Particle looks live on the FX prefabs.
public class Juice : MonoBehaviour
{
    public static Juice I;

    public ParticleSystem confetti, smoke, dust, debris, fire, sparkle;
    public Popup popupPrefab;

    static readonly Color[] Party = {
        new Color(1f, 0.3f, 0.35f), new Color(1f, 0.8f, 0.2f), new Color(0.3f, 0.85f, 0.4f),
        new Color(0.3f, 0.6f, 1f), new Color(0.85f, 0.4f, 1f), new Color(1f, 0.55f, 0.15f)
    };

    readonly Dictionary<Transform, Vector3> punchBase = new Dictionary<Transform, Vector3>();
    readonly Dictionary<Transform, Coroutine> punchRoutine = new Dictionary<Transform, Coroutine>();
    float stopUntil;
    bool stopping;

    void Awake()
    {
        if (I != null && I != this) { Destroy(this); return; }
        I = this;
        confetti = Live(confetti); smoke = Live(smoke); dust = Live(dust);
        debris = Live(debris); fire = Live(fire); sparkle = Live(sparkle);
    }

    // Fields may point at the FX prefab assets or at scene instances; emitting needs a scene instance.
    ParticleSystem Live(ParticleSystem ps) => ps == null || ps.gameObject.scene.IsValid() ? ps : Instantiate(ps, transform);

    ParticleSystem Fx(string kind)
    {
        switch (kind)
        {
            case "confetti": return confetti;
            case "smoke": return smoke;
            case "dust": return dust;
            case "debris": return debris;
            case "fire": return fire;
            case "sparkle": return sparkle;
            default: return null;
        }
    }

    // ---------- static API ----------

    public static void Shake(float strength, float duration)
    {
        if (CameraRig.I != null) CameraRig.I.Shake(strength, duration);
    }

    public static void HitStop(float seconds)
    {
        if (I == null || seconds <= 0f) return;
        if (Game.I != null && Game.I.State == GameState.Paused) return;
        if (!I.stopping && Time.timeScale == 0f) return; // someone else froze time (pause)
        // Overlapping hit stops extend each other instead of stacking.
        I.stopUntil = Mathf.Max(I.stopUntil, Time.unscaledTime + Mathf.Min(seconds, 0.25f));
        if (!I.stopping) I.StartCoroutine(I.HitStopRoutine());
    }

    public static void Burst(string kind, Vector2 pos, Color color, int count = 12)
    {
        if (I == null || count <= 0) return;
        var ps = I.Fx(kind);
        if (ps == null) return;
        var ep = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            I.Fill(ref ep, kind, pos, color);
            ps.Emit(ep, 1);
        }
    }

    public static void Popup(string text, Vector2 pos, Color color, float size = 1f)
    {
        if (I == null) return;
        var p = Instantiate(I.popupPrefab, pos, Quaternion.identity);
        p.Show(text, color, size);
    }

    public static void Punch(Transform t, float amount = 0.25f, float duration = 0.25f)
    {
        if (I == null || t == null) return;
        if (I.punchRoutine.TryGetValue(t, out var running) && running != null) I.StopCoroutine(running);
        if (!I.punchBase.ContainsKey(t)) I.punchBase[t] = t.localScale;
        I.punchRoutine[t] = I.StartCoroutine(I.PunchRoutine(t, amount, duration));
    }

    IEnumerator HitStopRoutine()
    {
        stopping = true;
        Time.timeScale = 0f;
        while (Time.unscaledTime < stopUntil) yield return null;
        stopping = false;
        bool paused = Game.I != null && Game.I.State == GameState.Paused;
        if (!paused) Time.timeScale = 1f;
    }

    // Per-particle randomness for each burst kind.
    void Fill(ref ParticleSystem.EmitParams ep, string kind, Vector2 pos, Color color)
    {
        Vector2 dir = Random.insideUnitCircle.normalized;
        float speed, sz, life, spin = Random.Range(-90f, 90f);
        Color c = color;
        switch (kind)
        {
            case "confetti":
                dir = Angle(30f, 150f); speed = Random.Range(5f, 11f); sz = Random.Range(0.15f, 0.28f);
                life = Random.Range(1.2f, 2f); spin = Random.Range(-720f, 720f);
                if (Random.value < 0.5f) c = Party[Random.Range(0, Party.Length)];
                break;
            case "smoke":
                speed = Random.Range(0.5f, 2.5f); sz = Random.Range(0.6f, 1.2f); life = Random.Range(0.8f, 1.4f);
                c.a *= 0.85f;
                break;
            case "dust":
                dir = new Vector2(Random.value < 0.5f ? -1f : 1f, Random.Range(0.15f, 0.6f)).normalized;
                speed = Random.Range(1f, 3f); sz = Random.Range(0.4f, 0.8f); life = Random.Range(0.5f, 0.9f);
                c.a *= 0.8f;
                break;
            case "sparkle":
                speed = Random.Range(2f, 6f); sz = Random.Range(0.15f, 0.35f); life = Random.Range(0.4f, 0.8f);
                spin = Random.Range(-360f, 360f);
                c = Color.Lerp(color, Color.white, Random.Range(0f, 0.5f));
                break;
            case "debris":
                dir = Angle(20f, 160f); speed = Random.Range(4f, 10f); sz = Random.Range(0.12f, 0.3f);
                life = Random.Range(1f, 1.6f); spin = Random.Range(-900f, 900f);
                c = color * Random.Range(0.6f, 1f); c.a = 1f;
                break;
            case "fire":
                speed = Random.Range(1f, 4f); sz = Random.Range(0.5f, 1.1f); life = Random.Range(0.3f, 0.6f);
                c = Color.Lerp(Color.Lerp(new Color(1f, 0.95f, 0.3f), new Color(1f, 0.5f, 0.1f), Random.value), color, 0.2f);
                break;
            default:
                speed = Random.Range(1f, 4f); sz = 0.3f; life = 0.6f;
                break;
        }
        ep.position = (Vector3)(pos + Random.insideUnitCircle * 0.15f);
        ep.velocity = dir * speed;
        ep.startSize = sz;
        ep.startLifetime = life;
        ep.startColor = c;
        ep.rotation = Random.Range(0f, 360f);
        ep.angularVelocity = spin;
    }

    static Vector2 Angle(float minDeg, float maxDeg)
    {
        float a = Random.Range(minDeg, maxDeg) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
    }

    IEnumerator PunchRoutine(Transform t, float amount, float duration)
    {
        Vector3 baseScale = punchBase[t];
        float time = 0f;
        while (time < duration)
        {
            if (t == null) break;
            time += Time.deltaTime;
            float p = Mathf.Clamp01(time / duration);
            // Decaying wobble: wide+short, then tall+thin, settling back.
            float s = Mathf.Sin(p * Mathf.PI * 3f) * (1f - p) * amount;
            t.localScale = new Vector3(baseScale.x * (1f + s), baseScale.y * (1f - s), baseScale.z);
            yield return null;
        }
        if (t != null) t.localScale = baseScale;
        punchBase.Remove(t);
        punchRoutine.Remove(t);
    }
}
