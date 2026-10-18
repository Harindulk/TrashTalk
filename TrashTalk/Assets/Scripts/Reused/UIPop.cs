using System.Collections;
using UnityEngine;

// Pops in on enable (unscaled time) and optionally bobs, for title letters, headings and panels.
public class UIPop : MonoBehaviour
{
    public float delay = 0f;     // < 0: no pop-in
    public bool bob;
    public int bobIndex;         // phase offset between neighbouring letters
    public float tilt;           // resting rotation while bobbing

    RectTransform rt;
    Vector2 home;

    void Awake()
    {
        rt = (RectTransform)transform;
        home = rt.anchoredPosition;
    }

    void OnEnable()
    {
        if (delay >= 0f) StartCoroutine(Pop());
    }

    IEnumerator Pop()
    {
        transform.localScale = Vector3.zero;
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        for (float a = 0f; a < 1f;)
        {
            a += Time.unscaledDeltaTime / 0.35f;
            transform.localScale = Vector3.one * UI.EaseOutBack(Mathf.Clamp01(a));
            yield return null;
        }
    }

    void Update()
    {
        if (!bob) return;
        float t = Time.unscaledTime;
        rt.anchoredPosition = home + new Vector2(0, Mathf.Sin(t * 3f + bobIndex * 0.6f) * 16f);
        rt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 2f + bobIndex * 1.3f) * 7f + tilt);
    }
}
