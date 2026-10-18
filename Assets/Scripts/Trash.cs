using System.Collections.Generic;
using System.Collections;
using UnityEngine;

// One pre-placed piece of trash: waits in the pile, rides a rocket, then tumbles into a can (hides) or becomes litter (walks back to the pile).
public class Trash : MonoBehaviour
{
    public TrashType Type;
    public Rigidbody2D Body;
    public bool Done; // true unless flying: pile/stacked props are ignored by physics, cans and the rest/age rules
    public static readonly List<Trash> All = new List<Trash>(); // live pieces, so cans can watch them

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    [Header("Tuning")]
    public float restSpeed = 0.2f;
    public float restTime = 1.2f;
    public float killY = -3f;
    public float slurpRange = 2.2f;  // open cans pull in wanted trash this close (sideways)
    public float slurpForce = 20f;

    [Header("Wiring")]
    public Sprite[] sprites;          // indexed by (int)TrashType
    public SpriteRenderer sprite;
    public CircleCollider2D circle;   // Paper, Apple
    public BoxCollider2D box;         // everything else

    float restTimer;
    float lastPunch;

    Transform homeParent;
    Vector3 homePos, homeScale;
    Quaternion homeRot;
    int homeOrder;

    void Awake()
    {
        ApplyType();
        homeParent = transform.parent;
        homePos = transform.localPosition;
        homeRot = transform.localRotation;
        homeScale = transform.localScale;
        homeOrder = sprite.sortingOrder;
        Done = true;
        Body.simulated = false;
    }

    void OnValidate() { if (sprite != null && circle != null && box != null && sprites != null && (int)Type < sprites.Length && sprites[(int)Type] != null) ApplyType(); }

    // Sprite and collider for Type (Inspector, editor helper and Awake).
    public void ApplyType()
    {
        Vector2 size = TrashInfo.Size(Type);
        sprite.sprite = sprites[(int)Type];
        Vector2 b = sprite.sprite.bounds.size;
        sprite.transform.localScale = new Vector3(size.x / b.x, size.y / b.y, 1f);

        bool round = Type == TrashType.Paper || Type == TrashType.Apple;
        circle.enabled = round;
        box.enabled = !round;
        circle.radius = Mathf.Max(size.x, size.y) * 0.45f;
        box.size = size * 0.9f;
    }

    // Parks it as a still prop (on a rocket's back or back in the pile).
    public void Hold(Transform parent, Vector3 localPos, Quaternion localRot, Vector3 localScale, int order)
    {
        StopAllCoroutines(); // a littered piece can get reloaded mid-fade
        Done = true;
        Body.simulated = false;
        transform.SetParent(parent, false);
        transform.localPosition = localPos;
        transform.localRotation = localRot;
        transform.localScale = localScale;
        sprite.sortingOrder = order;
        sprite.color = Color.white;
    }

    void GoHome() => Hold(homeParent, homePos, homeRot, homeScale, homeOrder);

    // Off the rocket and into play.
    public void Fly(Vector2 pos, Vector2 velocity, float spin = 0f)
    {
        transform.SetParent(homeParent, false);
        transform.SetPositionAndRotation(pos, Quaternion.Euler(0, 0, Random.Range(0f, 360f)));
        transform.localScale = Vector3.one;
        sprite.sortingOrder = Order.Trash;
        Done = false;
        age = restTimer = 0f;
        Body.simulated = true;
        Body.position = pos;
        Body.linearVelocity = velocity;
        Body.angularVelocity = spin != 0f ? spin : Random.Range(-240f, 240f);

        Juice.Punch(sprite.transform, 0.3f, 0.2f);
        if (Game.I != null) Game.I.OnTrashFlying(this);
    }

    float age;

    // Open cans "slurp" nearby trash they want, so near misses still go in.
    void FixedUpdate()
    {
        if (Done) return;
        Vector2 p = Body.position;
        foreach (var c in TrashCan.All)
        {
            if (c == null || !c.LidOpen || !c.Accepts(Type)) continue;
            Vector2 m = c.MouthPos;
            float dx = m.x - p.x, dy = p.y - m.y;
            if (dy < -0.2f || dy > 3.5f || Mathf.Abs(dx) > slurpRange) continue;
            Body.AddForce(new Vector2(dx * slurpForce - Body.linearVelocity.x * 2f, 0f) * Body.mass);
            break;
        }
    }

    void Update()
    {
        if (Done || Time.timeScale == 0f) return;
        // Fans can hold a piece in the air forever, so give up eventually.
        if ((age += Time.deltaTime) > 12f) { Litter(); return; }

        Vector2 p = Body.position;
        if (p.y < killY || OutOfBounds(p)) { Litter(); return; }

        if (Body.linearVelocity.magnitude < restSpeed)
        {
            restTimer += Time.deltaTime;
            if (restTimer > restTime) Litter();
        }
        else restTimer = 0f;
    }

    bool OutOfBounds(Vector2 p)
    {
        if (Game.I == null || Game.I.Root == null) return false;
        Rect b = Game.I.Root.Bounds;
        return p.x < b.xMin || p.x > b.xMax;
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (Done) return;
        if (c.collider.CompareTag("Ground")) { Litter(); return; }

        float hit = c.relativeVelocity.magnitude;
        if (hit > 3f && Time.time - lastPunch > 0.25f)
        {
            lastPunch = Time.time;
            Juice.Punch(sprite.transform, Mathf.Min(hit * 0.04f, 0.35f), 0.15f);
            // Cans play their own rim/bonk sounds.
            if (c.collider.GetComponentInParent<TrashCan>() == null) Sfx.Play("bonk", 0.35f, 1.6f);
        }
    }

    public void Score(TrashCan can)
    {
        if (Done) return;
        Done = true;
        Body.simulated = false;
        sprite.sortingOrder = Order.Cans - 1; // slide in behind the can body
        if (can != null) transform.SetParent(can.transform, true);
        if (Game.I != null) Game.I.OnTrashScored(this, can);
        StartCoroutine(Gulp(can != null));
    }

    IEnumerator Gulp(bool inCan)
    {
        Vector3 start = transform.localPosition;
        Vector3 drop = transform.parent != null ? transform.parent.InverseTransformVector(Vector3.down * 0.9f) : Vector3.down * 0.9f;
        Vector3 end = (inCan ? new Vector3(0f, start.y, 0f) : start) + drop;
        Vector3 s0 = transform.localScale;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.3f)
        {
            float e = t * t;
            transform.localPosition = Vector3.Lerp(start, end, e);
            transform.localScale = s0 * (1f - 0.8f * e);
            transform.Rotate(0f, 0f, 720f * Time.deltaTime);
            yield return null;
        }
        gameObject.SetActive(false); // hides in the can
    }

    public void Reject(Vector2 spitVelocity)
    {
        if (Done) return;
        Body.linearVelocity = spitVelocity;
        Body.angularVelocity = Random.Range(-600f, 600f);
        restTimer = 0f;
        Sfx.Play("spit");
        Juice.Punch(sprite.transform, 0.35f, 0.2f);
        lastPunch = Time.time;
    }

    public void Litter()
    {
        if (Done) return;
        Done = true;
        Vector2 p = transform.position;
        sprite.color = new Color(0.55f, 0.55f, 0.55f);
        Juice.Popup("MISS", p + Vector2.up * 0.6f, new Color(0.94f, 0.33f, 0.36f), 1f);
        Juice.Burst("dust", p, new Color(0.60f, 0.37f, 0.22f), 10);
        Sfx.Play("miss");
        if (Game.I != null) Game.I.OnTrashLittered(this);
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(0.6f);
        Color c = sprite.color;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f)
        {
            c.a = 1f - t;
            sprite.color = c;
            yield return null;
        }
        GoHome();
    }
}
