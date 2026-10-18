using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A trash can with a face. The lid is its mouth. It watches, chomps, spits, mocks and naps.
// Prefabs: Assets/Prefabs/Cans/Can_<Kind>.prefab (root pivot at the can's bottom-center).
// Visual life (breathing, blinks, brows, chomp, shake, laugh, lid) is Art/Animations/Can.controller:
// bools Open/Talk/Asleep/Scared hold states, triggers Hop/Chomp/Angry/Bonk/Laugh/Cheer (body) and Gulp (lid) punctuate.
public class TrashCan : MonoBehaviour
{
    public static readonly List<TrashCan> All = new List<TrashCan>();

    [System.Serializable] public class Talk { public string evt; public string[] lines; }

    public CanKind Kind;
    public bool LidOpen;

    [Header("Character")]
    public float w = 1.3f, h = 1.6f;   // body size in world units
    public Color tint = Color.white;   // confetti / bubble tint
    public float voice = 1f;
    public Talk[] talk;                // what it says, per event (hello, near, score, reject, miss, rim, win, lose, nap, wake, scared, hit, bossIntro)

    [Header("Feel")]
    public float openRange = 4.5f, lookRange = 14f, talkCooldown = 1.6f;
    public float napTime = 3f, awakeTime = 2f;   // Lazy only

    [Header("Parts")]
    public SpriteRenderer bubble;      // inactive SpeechBubble child; its parent is the bubble root
    public TextMesh bubbleText;
    public Transform lidHinge, pupilL, pupilR;
    public BoxCollider2D lidCol;

    // Mouth point in world space (top center of the can). Trash can tween here.
    public Vector2 MouthPos => (Vector2)transform.position + new Vector2(0, h);

    [HideInInspector] public Animator anim;
    BossCan boss;
    float s, talkT, nextTalk, stateT, zzzT, fleeOffset, fleeT, fleeDir = 1f, lastRejectTime;
    bool asleep, wasOpen;
    Vector2 lookOff;
    Trash lastReject;
    Rocket greetedRocket;
    string lastLine;
    Coroutine talking;
    static float lastNearLine = -10f;

    void Awake()
    {
        All.Add(this);
        s = Mathf.Min(w, h);
        anim = GetComponent<Animator>();
        boss = GetComponent<BossCan>();
        anim.Play("Idle", 0, Random.value); // desync breathing/blinks between cans
        if (Kind == CanKind.Lazy) { asleep = Random.value < 0.5f; stateT = Random.Range(0.5f, 2f); }
    }

    void OnDestroy() { All.Remove(this); }

    IEnumerator Start()
    {
        int i = Mathf.Max(0, All.IndexOf(this));
        yield return new WaitForSeconds(Mathf.Min(2.8f, 0.4f + i * 0.55f + Random.value * 0.3f));
        if (boss != null) { boss.Intro(); yield break; }
        Say(Line("hello"));
        anim.SetTrigger("Hop");
    }

    public bool Accepts(TrashType t) =>
        Kind == CanKind.Snobby ? TrashInfo.Category(t) == TrashCategory.Recycle :
        Kind == CanKind.Compost ? TrashInfo.Category(t) == TrashCategory.Food : true;

    public void Say(string text)
    {
        nextTalk = Time.time + talkCooldown;
        talkT = Mathf.Min(1.5f, 0.25f + text.Length * 0.04f);
        if (bubble == null || string.IsNullOrEmpty(text)) return;
        if (talking != null) StopCoroutine(talking);
        talking = StartCoroutine(Bubble(text));
    }

    public void Chat(string evt, bool force = false) { if (force || Time.time >= nextTalk) Say(Line(evt)); }

    public string Line(string evt)
    {
        foreach (var t in talk)
        {
            if (t.evt != evt || t.lines.Length == 0) continue;
            string pick = t.lines[Random.Range(0, t.lines.Length)];
            if (pick == lastLine && t.lines.Length > 1) pick = t.lines[(System.Array.IndexOf(t.lines, pick) + 1) % t.lines.Length];
            return lastLine = pick;
        }
        return "...";
    }

    // Pops in, typewrites (hidden part stays in the string, transparent, so the layout never jumps), wobbles, shrinks away.
    IEnumerator Bubble(string text)
    {
        var root = bubble.transform.parent;
        string full = Wrap(text, 18, out int lines, out int longest);
        float dur = 2.2f + text.Length / 40f;
        float bw = Mathf.Max(1.4f, longest * 0.16f + 0.7f), bh = lines * 0.32f + 0.6f;
        Vector2 size = bubble.sprite.bounds.size;
        bubble.transform.localScale = new Vector3(bw / size.x, bh / size.y, 1f);
        bubble.color = Color.Lerp(Color.white, tint, 0.15f);
        bubbleText.transform.localPosition = new Vector3(0, bh * 0.08f, 0);
        root.localPosition = new Vector3(bw * 0.35f, 1.4f, 0); // shift right so the bottom-left tail points at the can
        root.gameObject.SetActive(true);
        if (Sfx.I != null) Sfx.Talk(voice, Mathf.Clamp(text.Length / 3, 2, 10));

        int shown = -1;
        for (float t = 0f; t < dur + 0.18f; t += Time.deltaTime)
        {
            int n = Mathf.Min(full.Length, Mathf.FloorToInt(t * 40f));
            if (n != shown) bubbleText.text = (shown = n) >= full.Length ? full : full.Substring(0, n) + "<color=#00000000>" + full.Substring(n) + "</color>";
            float x = t / 0.25f - 1f; // ease-out-back pop
            float k = t < 0.25f ? 1f + 3.2f * x * x * x + 2.2f * x * x : t > dur ? 1f - (t - dur) / 0.18f : 1f;
            float wob = Mathf.Sin(t * 7f) * 0.03f;
            root.localScale = new Vector3(k * (1 + wob), k * (1 - wob), 1);
            root.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 4f) * 3f);
            yield return null;
        }
        root.gameObject.SetActive(false);
    }

    static string Wrap(string text, int max, out int lines, out int longest)
    {
        var sb = new System.Text.StringBuilder();
        int len = 0;
        lines = 1; longest = 0;
        foreach (var word in text.Split(' '))
        {
            if (len > 0 && len + 1 + word.Length > max) { sb.Append('\n'); lines++; longest = Mathf.Max(longest, len); len = 0; }
            else if (len > 0) { sb.Append(' '); len++; }
            sb.Append(word);
            len += word.Length;
        }
        longest = Mathf.Max(longest, len);
        return sb.ToString();
    }

    public static void BroadcastMiss(Vector2 pos)
    {
        TrashCan best = null;
        float bestDist = 8f;
        foreach (var c in All)
        {
            float d = Vector2.Distance(c.transform.position, pos);
            if (d < bestDist) { bestDist = d; best = c; }
        }
        if (best != null) best.Mock();
        // Dumpsters heckle every miss, wherever it lands.
        foreach (var c in All) if (c != best && (c.Kind == CanKind.Dumpster || c.Kind == CanKind.Boss)) c.Mock();
    }

    public static void BroadcastWin() { for (int i = 0; i < All.Count; i++) All[i].StartCoroutine(All[i].React("win", i * 0.25f)); }
    public static void BroadcastLose() { for (int i = 0; i < All.Count; i++) All[i].StartCoroutine(All[i].React("lose", i * 0.25f)); }

    public static void Snd(string name, float vol = 1f, float pitch = 1f) { if (Sfx.I != null) Sfx.Play(name, vol, pitch); }

    IEnumerator React(string evt, float delay)
    {
        yield return new WaitForSeconds(delay);
        asleep = false;
        if (evt == "lose") { anim.SetTrigger("Laugh"); Snd("laugh", 0.8f, voice); }
        else if (boss != null) anim.SetTrigger("Angry"); // the boss sulks when you win
        else
        {
            anim.SetTrigger("Cheer");
            if (Juice.I != null) Juice.Burst("confetti", MouthPos + Vector2.up * 0.5f, tint, 20);
        }
        Say(Line(evt));
    }

    void Mock() { anim.SetTrigger("Laugh"); Snd("laugh", 0.7f, voice); Chat("miss", true); }

    void OnTriggerEnter2D(Collider2D other) { TryEat(other); }
    void OnTriggerStay2D(Collider2D other) { TryEat(other); }

    void TryEat(Collider2D other)
    {
        if (!LidOpen || other.attachedRigidbody == null) return;
        var t = other.attachedRigidbody.GetComponent<Trash>();
        if (t == null || t.Done) return;
        if (t == lastReject && Time.time < lastRejectTime + 0.7f) return;
        if (other.attachedRigidbody.linearVelocity.y > 0.5f) return;

        if (Accepts(t.Type)) { t.Score(this); anim.SetTrigger("Gulp"); StartCoroutine(Chomp()); }
        else Spit(t, other.attachedRigidbody.linearVelocity);
    }

    IEnumerator Chomp()
    {
        yield return new WaitForSeconds(0.15f); // Gulp holds the lid open a beat while it drops in, then slams
        Snd("clunk");
        if (Juice.I != null)
        {
            Juice.Burst("confetti", MouthPos + Vector2.up * 0.3f, tint, 16);
            Juice.Popup("+1", MouthPos + Vector2.up * 0.8f, new Color(1.00f, 0.71f, 0.18f), 1.2f);
        }
        if (boss != null) { boss.Hit(); yield break; }
        anim.SetTrigger("Chomp");
        if (Random.value < 0.75f) Chat("score", true);
    }

    void Spit(Trash t, Vector2 vel)
    {
        lastReject = t;
        lastRejectTime = Time.time;
        // Spit it back the way it came.
        float side = vel.x > 0.3f ? -1f : vel.x < -0.3f ? 1f : (t.transform.position.x < transform.position.x ? -1f : 1f);
        t.Reject(new Vector2(side * Random.Range(2.5f, 4f), Random.Range(6f, 8f)));
        anim.SetTrigger("Angry");
        if (Juice.I != null) Juice.Burst("smoke", MouthPos, new Color(0.8f, 0.8f, 0.7f), 6);
        Chat("reject", true);
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (c.rigidbody == null || c.relativeVelocity.magnitude < 1.5f) return;
        var t = c.rigidbody.GetComponent<Trash>();
        if (t == null || t.Done) return;
        anim.SetTrigger("Bonk");
        if (c.otherCollider == lidCol) { Snd("bonk"); if (asleep) Chat("nap"); return; }
        Vector2 p = c.contactCount > 0 ? c.GetContact(0).point : (Vector2)c.transform.position;
        if (p.y > transform.position.y + h - 0.35f) { Snd("rim"); Chat("rim"); }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        Vector2 mouthPos = MouthPos;

        // Nearest thing worth looking at.
        float nearest = float.MaxValue;
        Vector2 nearestPos = Vector2.zero;
        foreach (var t in Trash.All)
        {
            if (t == null || t.Done) continue;
            float d = Vector2.Distance(t.transform.position, mouthPos);
            if (d < nearest) { nearest = d; nearestPos = t.transform.position; }
        }
        var rocket = Game.I != null ? Game.I.ActiveRocket : null;
        if (rocket != null && (!rocket.Launched || rocket.Dead)) rocket = null;
        float rocketDist = rocket != null ? Vector2.Distance(rocket.transform.position, mouthPos) : float.MaxValue;
        if (rocketDist < nearest) { nearest = rocketDist; nearestPos = rocket.transform.position; }
        Vector2 lookAt = nearest < lookRange ? nearestPos : Inp.WorldPos; // idle: watch the player's cursor

        if (rocket != null && rocketDist < 6f && greetedRocket != rocket && Kind != CanKind.Nervous)
        {
            greetedRocket = rocket;
            if (Time.time > lastNearLine + 3f && !asleep) { lastNearLine = Time.time; Chat("near"); }
        }

        bool open = nearest < openRange + w * 0.5f; // a snack in reach: mouth open
        if (Kind == CanKind.Lazy) { UpdateNap(dt); open = !asleep; }
        else if (boss != null) open = boss.RhythmOpen;
        else if (Kind == CanKind.Nervous) UpdateNervous(dt, rocket, rocketDist);
        talkT -= dt;
        anim.SetBool("Open", open);
        anim.SetBool("Talk", talkT > 0f);
        anim.SetBool("Asleep", asleep);
        anim.SetBool("Scared", fleeT > 0f);

        // The Animator owns the lid; gameplay reads its angle back (last frame's pose; closing bounce dips below 0 = ~350).
        float a = lidHinge.localEulerAngles.z;
        LidOpen = a > 60f && a < 200f;
        lidCol.enabled = a < 20f || a > 300f;
        if (LidOpen && !wasOpen && boss == null) Snd("lid", 0.35f, voice);
        wasOpen = LidOpen;

        // Pupils sit at their eye's origin under rigs the clips squash for blinks/fright; only the look offset is code.
        Vector2 dir = lookAt - ((Vector2)transform.position + new Vector2(0, h * 0.6f));
        lookOff = Vector2.Lerp(lookOff, Vector2.ClampMagnitude(dir * 0.15f, 0.075f * s), dt * 10f);
        pupilL.localPosition = pupilR.localPosition = lookOff;
    }

    void UpdateNap(float dt)
    {
        if ((stateT -= dt) <= 0f)
        {
            asleep = !asleep;
            stateT = asleep ? napTime : awakeTime;
            if (asleep) { if (Random.value < 0.35f) Chat("nap"); }
            else { anim.SetTrigger("Hop"); Snd("boing", 0.5f, voice); if (Random.value < 0.5f) Chat("wake"); }
        }
        if (asleep && (zzzT -= dt) <= 0f)
        {
            zzzT = 1f;
            if (Juice.I != null) Juice.Popup("Z", MouthPos + new Vector2(w * 0.4f, 0.4f), new Color(1.00f, 0.96f, 0.87f), 0.8f);
        }
    }

    void UpdateNervous(float dt, Rocket rocket, float rocketDist)
    {
        if (rocket != null && rocketDist < 5f)
        {
            if (fleeT <= 0f)
            {
                fleeDir = rocket.transform.position.x < transform.position.x ? 1f : -1f;
                anim.SetTrigger("Hop");
                Snd("boing", 0.7f, 1.4f);
                Chat("scared", true);
            }
            fleeT = Mathf.Max(fleeT, 1.2f);
        }
        if (fleeT > 0f) fleeT -= dt;
        float next = Mathf.MoveTowards(fleeOffset, fleeT > 0f ? fleeDir * 2.5f : 0f, (fleeT > 0f ? 9f : 2.5f) * dt);
        transform.position += Vector3.right * (next - fleeOffset); // delta, so it still works if a Mover is attached
        fleeOffset = next;
    }
}
