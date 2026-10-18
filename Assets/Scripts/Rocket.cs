using System.Collections.Generic;
using UnityEngine;

// The junk rocket: burns fuel on its own, nose chases the mouse, drops trash on click, crashes funny.
public class Rocket : MonoBehaviour
{
    public List<Trash> Load = new List<Trash>(); // strapped to its back, last = top
    public float Fuel01 = 1f;
    public bool Launched, Dead;
    public Rigidbody2D Body;
    public Transform Visual; // squash/tilt pivot at the rocket's bottom

    [Header("Tuning")]
    public float thrust = 13f;       // acceleration along the nose when empty
    public float damping = 1f;
    public float burnTime = 3.2f;
    public float turnSpeed = 160f;
    public float tapTime = 0.2f;     // shorter press = drop, longer = steer
    public float crashSpeed = 2.5f;
    public float dropHop = 1.5f;
    public float dropCarry = 0.35f;  // share of rocket speed the dropped trash keeps

    public float Mass => 1f + 0.35f * Load.Count;
    // Heavier = a bit slower, so each drop makes it zippier.
    public float Accel => thrust * (0.7f + 0.3f / Mass);

    [Header("Wiring")]
    public SpriteRenderer flame;
    public ParticleSystem trail;       // RocketTrail child, starts on launch
    public SpriteRenderer[] sight;     // drop preview dots

    static readonly string[] crashWords = { "KABOOM!", "OOPS!", "WHOOPS!", "BONK!", "KAPOW!" };

    Vector3 flameScale;
    Vector3 visualBase;
    bool burning, pressOwned;
    float pressStart;
    float launchTime, stuckTime, turnRate, wobble, outOfFuelTime;

    void Awake()
    {
        visualBase = Visual.localPosition;
        flameScale = flame.transform.localScale;
        Body.simulated = false; // waiting in line / on the pad: a prop until launch
    }

    // Straps pile trash to its back like a backpack, last = top.
    public void Carry(List<Trash> load)
    {
        Load = load;
        float y = 0.15f;
        for (int i = 0; i < Load.Count; i++)
        {
            float h = TrashInfo.Size(Load[i].Type).y * 0.55f;
            Load[i].Hold(Visual, new Vector3(i % 2 == 0 ? -0.52f : -0.42f, y + h / 2f, 0f), Quaternion.identity, Vector3.one * 0.55f, Order.Rocket - 1);
            y += h * 0.9f;
        }
    }

    // Called by the Launcher while the player pulls back.
    public void Aim(Vector2 dir, float power01)
    {
        Visual.localRotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, dir));
        Visual.localScale = new Vector3(1f + 0.25f * power01, 1f - 0.3f * power01, 1f);
        Vector3 jitter = power01 > 0.97f ? (Vector3)(Random.insideUnitCircle * 0.05f) : Vector3.zero;
        Visual.localPosition = visualBase + jitter;
    }

    public void CancelAim()
    {
        ResetVisual();
        Juice.Punch(Visual, 0.2f, 0.2f);
        Sfx.Play("boing", 0.5f);
    }

    void ResetVisual()
    {
        Visual.localRotation = Quaternion.identity;
        Visual.localScale = Vector3.one;
        Visual.localPosition = visualBase;
    }

    public void Launch(Vector2 velocity)
    {
        if (Launched) return;
        Launched = true;
        launchTime = Time.time;
        ResetVisual();

        float a = Vector2.SignedAngle(Vector2.up, velocity);
        transform.rotation = Quaternion.Euler(0f, 0f, a);
        Body.simulated = true;
        Body.bodyType = RigidbodyType2D.Dynamic;
        Body.gravityScale = 1f;
        Body.linearDamping = damping;
        Body.mass = Mass;
        Body.rotation = a;
        Body.angularVelocity = 0f;
        Body.linearVelocity = velocity;

        burning = true;
        flame.enabled = true;
        if (trail != null) trail.Play();

        Vector2 butt = (Vector2)transform.position - (Vector2)transform.up * 1f;
        Juice.Burst("smoke", butt, Color.white, 18);
        Juice.Burst("fire", butt, new Color(1.00f, 0.71f, 0.18f), 8);
        Juice.Shake(0.35f, 0.25f);
        Juice.Punch(Visual, 0.4f, 0.3f);
        if (CameraRig.I != null) CameraRig.I.Punch(0.6f);
        Sfx.Play("launch");
        Sfx.Thrust(true);
        if (Game.I != null) Game.I.OnRocketLaunched(this);
    }

    void FixedUpdate()
    {
        if (!Launched || Dead) return;
        float dt = Time.fixedDeltaTime;
        float target = Body.rotation;

        if (burning)
        {
            // Flies straight along the aim (matches the dotted preview) unless the player steers.
            float steer = Inp.Steer;
            Vector2 toMouse = Inp.WorldPos - Body.position;
            if (steer != 0f) target = Body.rotation - steer * turnSpeed * dt;
            else if (Steering && toMouse.sqrMagnitude > 0.1f) target = Vector2.SignedAngle(Vector2.up, toMouse);

            Vector2 nose = Quaternion.Euler(0f, 0f, Body.rotation) * Vector3.up;
            Body.AddForce(nose * Accel * Body.mass);
        }
        else if (Body.linearVelocity.sqrMagnitude > 0.25f)
        {
            target = Vector2.SignedAngle(Vector2.up, Body.linearVelocity);
        }

        float next = Mathf.MoveTowardsAngle(Body.rotation, target, turnSpeed * dt);
        turnRate = Mathf.Abs(Mathf.DeltaAngle(Body.rotation, next)) / dt;
        Body.rotation = next;
        Body.angularVelocity = 0f;
    }

    void Update()
    {
        if (Dead || (Game.I != null && Game.I.State == GameState.Paused)) return;
        Wobble();
        if (!Launched) return;

        if (burning)
        {
            Fuel01 -= Time.deltaTime / burnTime;
            if (Fuel01 <= 0f) OutOfFuel();
        }

        if (flame.enabled)
        {
            bool sputter = Fuel01 < 0.2f && Random.value < 0.25f;
            float f = sputter ? 0.3f : Random.Range(0.8f, 1.25f);
            flame.transform.localScale = new Vector3(flameScale.x * Random.Range(0.9f, 1.1f), flameScale.y * f, 1f);
        }

        // Presses that started before launch (the slingshot pull) don't count.
        bool tap = false;
        if (Inp.Pressed) { pressOwned = true; pressStart = Time.unscaledTime; }
        if (Inp.Released && pressOwned) { tap = Time.unscaledTime - pressStart < tapTime; pressOwned = false; }
        if (Load.Count > 0 && Game.I != null && Game.I.State == GameState.Playing && (tap || Inp.DropKey)) Drop();

        CheckBounds();
        UpdateSight();
        if (Dead) return;

        // A dead-stick rocket resting on a ledge would sit forever, so blow it up.
        if (!burning && Body.linearVelocity.magnitude < 0.3f)
        {
            stuckTime += Time.deltaTime;
            if (stuckTime > 1.5f) Crash();
        }
        else stuckTime = 0f;
        // Fans can keep an empty rocket bobbing forever, so it always comes down eventually.
        if (!burning && Time.time - outOfFuelTime > 6f) Crash();
    }

    bool Steering => pressOwned && Inp.Held && Time.unscaledTime - pressStart >= tapTime;

    void Wobble()
    {
        float goal = 3f + (Launched ? Mathf.Min(turnRate * 0.08f, 18f) + (burning ? 3f : 0f) : 0f);
        wobble = Mathf.Lerp(wobble, goal, Time.deltaTime * 6f);
        for (int i = 0; i < Load.Count; i++)
        {
            float k = (i + 1f) / Load.Count; // top pieces sway more
            Load[i].transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 11f + i * 1.7f) * wobble * k);
        }
    }

    public void Drop()
    {
        var t = Load[Load.Count - 1];
        Load.RemoveAt(Load.Count - 1);
        t.Fly(DropPos, DropVelocity, Random.Range(-360f, 360f));
        Collider2D tc = t.circle.enabled ? t.circle : t.box;
        foreach (var c in GetComponents<Collider2D>()) Physics2D.IgnoreCollision(c, tc);

        Body.mass = Mass;
        Body.linearVelocity += Vector2.up * dropHop;
        Juice.Punch(Visual, 0.3f, 0.2f);
        Sfx.Play("drop", 1f, 1f + 0.08f * (5 - Load.Count));
        if (Load.Count == 0) Juice.Popup("EMPTY!", Body.position + Vector2.up * 1.3f, new Color(1.00f, 0.96f, 0.87f), 0.8f);
    }

    public Vector2 DropPos => Body.position + Vector2.down * 1.1f;
    public Vector2 DropVelocity => Body.linearVelocity * dropCarry + new Vector2(0f, -1.5f);

    // Faint dotted arc showing where the next piece will land.
    void UpdateSight()
    {
        bool show = Launched && !Dead && Load.Count > 0;
        Vector2 p = DropPos, v = DropVelocity;
        for (int i = 0; i < sight.Length; i++)
        {
            for (int k = 0; k < 4; k++) { v += Physics2D.gravity * 0.025f; p += v * 0.025f; }
            bool on = show && p.y > 0f;
            sight[i].enabled = on;
            sight[i].transform.position = p;
            sight[i].color = new Color(1f, 1f, 1f, 0.55f * (1f - i / (float)sight.Length));
        }
    }

    void OutOfFuel()
    {
        Fuel01 = 0f;
        outOfFuelTime = Time.time;
        StopBurn();
        flame.enabled = false;
        if (trail != null) trail.Stop();
        Juice.Burst("smoke", (Vector2)transform.position - (Vector2)transform.up * 1f, Color.gray, 8);
        Juice.Popup("sputter...", (Vector2)transform.position + Vector2.up * 1.2f, new Color(1.00f, 0.96f, 0.87f), 0.7f);
        Sfx.Play("pop", 0.6f, 0.6f);
    }

    void StopBurn()
    {
        if (burning) Sfx.Thrust(false);
        burning = false;
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (!Launched || Dead) return;
        if (c.collider.GetComponent<Trash>() != null) return;

        bool ground = c.collider.CompareTag("Ground");
        bool hard = c.relativeVelocity.magnitude > crashSpeed && Time.time - launchTime > 0.2f;
        if (ground || hard) Crash();
        else
        {
            Sfx.Play("bonk", 0.6f);
            Juice.Punch(Visual, 0.2f, 0.15f);
        }
    }

    void Crash()
    {
        if (Dead) return;
        Dead = true;
        Vector2 pos = Body.position;
        foreach (var c in GetComponents<Collider2D>()) c.enabled = false;

        Juice.Burst("fire", pos, new Color(1.00f, 0.71f, 0.18f), 20);
        Juice.Burst("debris", pos, new Color(0.94f, 0.33f, 0.36f), 14);
        Juice.Burst("smoke", pos, Color.gray, 16);
        Juice.Shake(0.5f, 0.4f);
        Juice.HitStop(0.08f);
        if (CameraRig.I != null) CameraRig.I.Punch(0.8f);
        Sfx.Play("crash");
        Juice.Popup(crashWords[Random.Range(0, crashWords.Length)], pos + Vector2.up, new Color(0.94f, 0.33f, 0.36f), 1.3f);

        foreach (var t in Load)
            t.Fly(pos + Vector2.up * 0.6f + Random.insideUnitCircle * 0.3f, new Vector2(Random.Range(-4f, 4f), Random.Range(4f, 8f)), Random.Range(-720f, 720f));
        Load.Clear();
        Finish();
    }

    void CheckBounds()
    {
        if (Game.I == null || Game.I.Root == null) return;
        Rect b = Game.I.Root.Bounds;
        Vector2 p = Body.position;
        if (p.x <= b.xMax + 6f && p.x >= b.xMin - 6f && p.y <= 40f) return;

        foreach (var t in Load) t.Fly(p, Vector2.zero);
        Load.Clear();
        Sfx.Play("whoosh");
        Finish();
    }

    void Finish()
    {
        Dead = true;
        StopBurn();
        if (trail != null)
        {
            // Let the smoke linger after the rocket is gone.
            trail.transform.SetParent(transform.parent, true);
            trail.Stop();
        }
        if (Game.I != null) Game.I.OnRocketFinished(this);
        gameObject.SetActive(false);
    }

    void OnDisable() => StopBurn();
}
