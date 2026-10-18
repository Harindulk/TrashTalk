using UnityEngine;

// Floating text that pops in, rises, wobbles and fades. Spawned by Juice.Popup from Assets/Prefabs/FX/Popup.prefab.
public class Popup : MonoBehaviour
{
    public TextMesh text;
    public TextMesh[] outlines;   // dark copies around the text fake a thick outline
    public float duration = 1f;
    public float rise = 1.3f;

    Color color;
    Vector2 start;
    float size = 1f, tilt, t;

    public void Show(string s, Color c, float size)
    {
        text.text = s;
        foreach (var o in outlines) o.text = s;
        color = c;
        this.size = size;
        start = transform.position;
        tilt = Random.Range(-12f, 12f);
        Update(); // first frame already animated, so it never flashes at full size
    }

    void Update()
    {
        t += Time.deltaTime;
        float p = t / duration;
        if (p >= 1f) { Destroy(gameObject); return; }

        // Pop in with overshoot, then settle.
        float s = p < 0.15f ? Mathf.Lerp(0f, 1.3f, p / 0.15f) : Mathf.Lerp(1.3f, 1f, Mathf.Clamp01((p - 0.15f) / 0.15f));
        transform.localScale = Vector3.one * (s * size);
        transform.position = start + Vector2.up * (rise * (1f - (1f - p) * (1f - p)));
        transform.rotation = Quaternion.Euler(0, 0, tilt * (1f - p) + Mathf.Sin(t * 12f) * 3f);

        float a = p < 0.6f ? 1f : 1f - (p - 0.6f) / 0.4f;
        text.color = new Color(color.r, color.g, color.b, color.a * a);
        foreach (var o in outlines) { var oc = o.color; oc.a = a; o.color = oc; }
    }
}
